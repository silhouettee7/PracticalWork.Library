using Domain.Abstractions.MessageBroker;
using Domain.Abstractions.Services;
using Domain.Events;
using Domain.Exceptions;
using Domain.Models;
using Domain.Options;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Dtos;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;
using LibraryServiceException = PracticalWork.Library.Exceptions.LibraryServiceException;

namespace PracticalWork.Library.Services;

public class LibraryService: ILibraryService
{
    private readonly IReaderRepository _readerRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IBorrowRepository _borrowRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICursorPaginationService<Book> _bookPaginationService;
    private readonly ICacheService _cacheService;
    private readonly IRabbitMqPublisher _publisher;
    private readonly string _booksCacheVersionKey;
    private readonly string _libraryBooksCachePrefix;
    private readonly double _libraryBooksCacheTtlInMinutes;
    private readonly string _booksDetailsCachePrefix;
    private readonly double _booksDetailsCacheTtlInMinutes;
    private readonly string _readersCacheVersionKey;
    private readonly string _libraryExchangeName;
    private readonly string _bookBorrowRoutingKey;
    private readonly string _bookReturnRoutingKey;
    private readonly string _coversBucketName;
    
    public LibraryService(IReaderRepository readerRepository, 
        IBookRepository bookRepository,
        IBorrowRepository borrowRepository,
        IFileStorageService fileStorageService,
        ICursorPaginationService<Book> bookPaginationService,
        IRabbitMqPublisher publisher,
        ICacheService cacheService,
        IOptionsMonitor<MinioOptions> minioOptions,
        IOptionsMonitor<RabbitOptions> rabbitOptions,
        IOptionsMonitor<RedisOptions> redisOptions)
    {
        _readerRepository = readerRepository;
        _bookRepository = bookRepository;
        _borrowRepository = borrowRepository;
        _fileStorageService = fileStorageService;
        _bookPaginationService = bookPaginationService;
        _cacheService = cacheService;
        _publisher = publisher;
        var minioOpt = minioOptions.CurrentValue;
        var redisOpt = redisOptions.CurrentValue;
        var rabbitOpt = rabbitOptions.CurrentValue;
        
        _booksCacheVersionKey = redisOpt.Books.VersionKey;
        _libraryBooksCachePrefix = redisOpt.Books.LibraryBooks.Prefix;
        _libraryBooksCacheTtlInMinutes = redisOpt.Books.LibraryBooks.TtlInMinutes;
        _booksDetailsCachePrefix = redisOpt.Books.BookDetails.Prefix;
        _booksDetailsCacheTtlInMinutes = redisOpt.Books.BookDetails.TtlInMinutes;
        _readersCacheVersionKey = redisOpt.Readers.VersionKey;
        _libraryExchangeName = rabbitOpt.Library.ExchangeName;
        _bookBorrowRoutingKey = rabbitOpt.Library.BookBorrow.RoutingKey;
        _bookReturnRoutingKey = rabbitOpt.Library.BookReturn.RoutingKey;
        _coversBucketName = minioOpt.CoversBucketName;
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
        var message = new BookBorrowedEvent(bookId, readerId, book.Title, 
            reader.FullName, bookBorrow.BorrowDate, bookBorrow.DueDate );
        await _publisher.PublishAsync(
            _libraryExchangeName,
            _bookBorrowRoutingKey,
            message);
        await _cacheService.InvalidateCache(_booksCacheVersionKey);
        await _cacheService.InvalidateCache(_readersCacheVersionKey);
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
            _libraryExchangeName, 
            _bookReturnRoutingKey, 
            message);
        await _cacheService.InvalidateCache(_booksCacheVersionKey);
        await _cacheService.InvalidateCache(_readersCacheVersionKey);
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
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_booksCacheVersionKey);
        var cacheKey = _cacheService.GenerateCacheKey(_libraryBooksCachePrefix, cacheVersion, request);
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
            TimeSpan.FromMinutes(_libraryBooksCacheTtlInMinutes));
            
        return cursorResponse;
    }

    private async Task<BookDetailsDto> GetBookDetails(Guid id, Book book)
    {
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_booksCacheVersionKey);
        var cacheKey = _cacheService.GenerateCacheKey(_booksDetailsCachePrefix, cacheVersion, null);
        var cachedResult = await _cacheService.GetAsync<BookDetailsDto>(cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }
        if (book.CoverImagePath is not null)
        {
            book.CoverImagePath = await _fileStorageService.GetFileLinkAsync(_coversBucketName,book.CoverImagePath);
        }
        var dto = new BookDetailsDto
        {
            Id = id,
            Book = book
        };
        await _cacheService.SetAsync(
            cacheKey,
            dto,
            TimeSpan.FromMinutes(_booksDetailsCacheTtlInMinutes));
        return dto;
    }
}