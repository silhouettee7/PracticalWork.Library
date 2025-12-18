using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Dtos;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Events;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Services;

public class LibraryService: ILibraryService
{
    private readonly IReaderRepository _readerRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IBorrowRepository _borrowRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICursorPaginationService<Book> _bookPaginationService;
    private readonly ICacheService _cacheService;
    private readonly IRabbitMQPublisher _publisher;
    private readonly MinioOptions _minioOptions;
    private readonly string _booksCacheVersion;
    private readonly string _libraryBooksPrefix;
    private readonly double _libraryBooksTtlInMinutes;
    private readonly string _booksDetailsPrefix;
    private readonly double _booksDetailsTtlInMinutes;
    private readonly string _readersCacheVersion;
    private readonly IConfigurationSection _rabbitLibrarySection;
    private readonly string _exchangeName;
    
    public LibraryService(IReaderRepository readerRepository, 
        IBookRepository bookRepository,
        IBorrowRepository borrowRepository,
        IFileStorageService fileStorageService,
        ICursorPaginationService<Book> bookPaginationService,
        IRabbitMQPublisher publisher,
        ICacheService cacheService,
        IConfiguration configuration,
        IOptionsMonitor<MinioOptions> minioOptions)
    {
        _readerRepository = readerRepository;
        _bookRepository = bookRepository;
        _borrowRepository = borrowRepository;
        _fileStorageService = fileStorageService;
        _bookPaginationService = bookPaginationService;
        _cacheService = cacheService;
        _publisher = publisher;
        _minioOptions = minioOptions.CurrentValue;
        var section = configuration.GetSection("App:Redis:Books");
        _booksCacheVersion = section["VersionKey"];
        _libraryBooksPrefix = section["LibraryBooks:Prefix"];
        _libraryBooksTtlInMinutes = section.GetValue<double>("LibraryBooks:TtlInMinutes");
        _booksDetailsPrefix = section["BookDetails:Prefix"];
        _booksDetailsTtlInMinutes = section.GetValue<double>("BookDetails:TtlInMinutes");
        _readersCacheVersion = configuration["App:Redis:Readers:VersionKey"];
        _rabbitLibrarySection = configuration.GetSection("App:RabbitMQ:Library");
        _exchangeName = _rabbitLibrarySection["ExchangeName"];
    }
    
    public async Task BorrowBook(Guid bookId, Guid readerId)
    {
        var book = await _bookRepository.GetBookById(bookId);
        var reader = await _readerRepository.GetReader(readerId);
        if (book.Status is BookStatus.Borrow or BookStatus.Archived)
        {
            throw new LibraryServiceException("Нельзя выдать архивную или выданную книгу");
        }

        if (!reader.IsActive)
        {
            throw new LibraryServiceException("Нельзя выдать книгу с неактивной карточкой");
        }
        var bookBorrow = BookBorrow.CreateBookBorrow();
        book.Status = BookStatus.Borrow;
        await _borrowRepository.CreateBookBorrow(bookId, readerId, bookBorrow);
        await _bookRepository.UpdateBook(bookId, book);
        var message = new BookBorrowedEvent(bookId, readerId, bookBorrow.Book.Title, 
            reader.FullName, bookBorrow.BorrowDate, bookBorrow.DueDate );
        await _publisher.PublishAsync(
            _exchangeName,
            _rabbitLibrarySection["BookBorrow:RoutingKey"],
            message);
        await _cacheService.InvalidateCache(_booksCacheVersion);
        await _cacheService.InvalidateCache(_readersCacheVersion);
    }

    public async Task ReturnBook(Guid bookId, Guid readerId)
    {
        var (id, bookBorrow) = await _borrowRepository.GetBookBorrow(bookId, readerId);
        var reader = await _readerRepository.GetReader(readerId);
        bookBorrow.ReturnBookBorrow();
        await _borrowRepository.ReturnBookBorrow(id, bookBorrow);
        var message = new BookReturnedEvent(bookId, readerId, bookBorrow.Book.Title, 
            reader.FullName, bookBorrow.ReturnDate);
        await _publisher.PublishAsync(
            _exchangeName, 
            _rabbitLibrarySection["BookReturn:RoutingKey"], 
            message);
        await _cacheService.InvalidateCache(_booksCacheVersion);
        await _cacheService.InvalidateCache(_readersCacheVersion);
    }

    public async Task<BookDetailsDto> GetBookDetails(Guid bookId)
    {
        var book = await _bookRepository.GetBookById(bookId);
        return await GetBookDetails(bookId, book);
    }

    public async Task<BookDetailsDto> GetBookDetails(string title)
    {
        var (id,book) = await _bookRepository.GetBookByTitle(title);
        return await GetBookDetails(id, book);
    }

    public async Task<CursorPaginationResponse<Book>> GetNonArchivedBooksPage(CursorPaginationRequest request)
    {
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_booksCacheVersion);
        var cacheKey = _cacheService.GenerateCacheKey(_libraryBooksPrefix, cacheVersion, request);
        var cachedResult = await _cacheService.GetAsync<CursorPaginationResponse<Book>>(cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }
            
        var books = await _bookRepository
            .GetNonArchivedBooksPageWithIssuanceRecords(request);
            
        var cursorResponse = _bookPaginationService.ToCursorPageResponse(books,request);
        await _cacheService.SetAsync(
            cacheKey,
            cursorResponse,
            TimeSpan.FromMinutes(_libraryBooksTtlInMinutes));
            
        return cursorResponse;
    }

    private async Task<BookDetailsDto> GetBookDetails(Guid id, Book book)
    {
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_booksCacheVersion);
        var cacheKey = _cacheService.GenerateCacheKey(_booksDetailsPrefix, cacheVersion, null);
        var cachedResult = await _cacheService.GetAsync<BookDetailsDto>(cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }
        if (book.CoverImagePath is not null)
        {
            book.CoverImagePath = await _fileStorageService.GetFileLinkAsync(_minioOptions.CoversBucketName,book.CoverImagePath);
        }
        var dto = new BookDetailsDto
        {
            Id = id,
            Book = book
        };
        await _cacheService.SetAsync(
            cacheKey,
            dto,
            TimeSpan.FromMinutes(_booksDetailsTtlInMinutes));
        return dto;
    }
}