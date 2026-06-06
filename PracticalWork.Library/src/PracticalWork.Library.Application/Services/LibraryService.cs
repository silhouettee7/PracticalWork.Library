using Domain.Abstractions.MessageBroker;
using Domain.Abstractions.Services;
using Domain.Events;
using Domain.Models;
using Domain.Options;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Dtos;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;
using LibraryServiceException = PracticalWork.Library.Exceptions.LibraryServiceException;

namespace PracticalWork.Library.Application.Services;

public class LibraryService: ILibraryService
{
    private readonly IReaderRepository _readerRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IBorrowRepository _borrowRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICursorPaginationService<Book> _bookPaginationService;
    private readonly ICacheService _cacheService;
    private readonly IRabbitMqPublisher _publisher;
    private readonly TimeProvider _timeProvider;
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
        IOptionsMonitor<RedisOptions> redisOptions, 
        TimeProvider timeProvider)
    {
        _readerRepository = readerRepository;
        _bookRepository = bookRepository;
        _borrowRepository = borrowRepository;
        _fileStorageService = fileStorageService;
        _bookPaginationService = bookPaginationService;
        _cacheService = cacheService;
        _timeProvider = timeProvider;
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
    
    public async Task BorrowBook(Guid bookId, Guid readerId, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetBookById(bookId, cancellationToken);
        var reader = await _readerRepository.GetReader(readerId, cancellationToken);
        if (book.Status is BookStatus.Borrow or BookStatus.Archived)
        {
            throw new LibraryServiceException("Нельзя выдать архивную или выданную книгу");
        }

        if (!reader.IsActive)
        {
            throw new LibraryServiceException("Нельзя выдать книгу с неактивной карточкой");
        }
        var bookBorrow = BookBorrow.CreateBookBorrow(_timeProvider);
        book.Status = BookStatus.Borrow;
        await _borrowRepository.CreateBookBorrow(bookId, readerId, bookBorrow, cancellationToken);
        await _bookRepository.UpdateBook(bookId, book, cancellationToken);
        var message = new BookBorrowedEvent(bookId, readerId, book.Title, 
            reader.FullName, bookBorrow.BorrowDate, bookBorrow.DueDate );
        await _publisher.PublishAsync(
            _libraryExchangeName,
            _bookBorrowRoutingKey,
            message, cancellationToken);
        await _cacheService.InvalidateCache(_booksCacheVersionKey);
        await _cacheService.InvalidateCache(_readersCacheVersionKey);
    }

    public async Task ReturnBook(Guid bookId, Guid readerId, CancellationToken cancellationToken)
    {
        var (id, bookBorrow) = await _borrowRepository.GetBookBorrow(bookId, readerId, cancellationToken);
        if (bookBorrow.Status != BookIssueStatus.Issued)
        {
            throw new LibraryServiceException("Нельзя вернуть уже возвращенную книгу");
        }

        if (bookBorrow.Book.Status != BookStatus.Borrow)
        {
            throw new LibraryServiceException("Книга не выдана читателю");
        }
        bookBorrow.ReturnBookBorrow(_timeProvider);
        await _borrowRepository.ReturnBookBorrow(id, bookBorrow, cancellationToken);
        var reader = await _readerRepository.GetReader(readerId, cancellationToken);
        var message = new BookReturnedEvent(bookId, readerId, bookBorrow.Book.Title, 
            reader.FullName, bookBorrow.ReturnDate);
        await _publisher.PublishAsync(
            _libraryExchangeName, 
            _bookReturnRoutingKey, 
            message, cancellationToken);
        await _cacheService.InvalidateCache(_booksCacheVersionKey);
        await _cacheService.InvalidateCache(_readersCacheVersionKey);
    }

    public async Task<BookDetailsDto> GetBookDetails(Guid bookId, CancellationToken cancellationToken)
    {
        async Task<(Guid bookId, Book)> GetBook() => 
            (bookId, await _bookRepository.GetBookById(bookId, cancellationToken));

        return await GetBookDetails((Func<Task<(Guid bookId, Book)>>)GetBook, cancellationToken);
    }
    
    public async Task<BookDetailsDto> GetBookDetails(string title, CancellationToken cancellationToken)
    {
        Task<(Guid id, Book book)> GetBook() => 
            _bookRepository.GetBookByTitle(title, cancellationToken);
        
        return await GetBookDetails(GetBook, cancellationToken);
    }

    public async Task<CursorPaginationResponse<Book>> GetNonArchivedBooksPage(
        CursorPaginationRequest request, CancellationToken cancellationToken)
    {
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_booksCacheVersionKey);
        var cacheKey = _cacheService.GenerateCacheKey(_libraryBooksCachePrefix, cacheVersion, request);
        var cachedResult = await _cacheService.GetAsync<CursorPaginationResponse<Book>>(cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }
            
        var books = await _bookRepository
            .GetNonArchivedBooksPageWithIssuanceRecords(request, cancellationToken);
            
        var cursorResponse = _bookPaginationService.ToCursorPageResponse(books,request);
        await _cacheService.SetAsync(
            cacheKey,
            cursorResponse,
            TimeSpan.FromMinutes(_libraryBooksCacheTtlInMinutes));
            
        return cursorResponse;
    }

    private async Task<BookDetailsDto> GetBookDetails(Func<Task<(Guid id, Book book)>> getBook, CancellationToken cancellationToken)
    {
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_booksCacheVersionKey);
        var cacheKey = _cacheService.GenerateCacheKey(_booksDetailsCachePrefix, cacheVersion, null);
        var cachedResult = await _cacheService.GetAsync<BookDetailsDto>(cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var (id, book) = await getBook();
        var dto = new BookDetailsDto
        {
            Id = id,
            Book = book
        };
        
        if (book.CoverImagePath is null) return dto;
        
        book.CoverImagePath = await _fileStorageService.GetFileLinkAsync(_coversBucketName,
            book.CoverImagePath, cancellationToken);
        await _cacheService.SetAsync(
            cacheKey,
            dto,
            TimeSpan.FromMinutes(_booksDetailsCacheTtlInMinutes));
        return dto;
    }
}