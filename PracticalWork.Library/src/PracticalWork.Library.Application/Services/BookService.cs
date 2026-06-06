using Domain.Abstractions.MessageBroker;
using Domain.Abstractions.Services;
using Domain.Events;
using Domain.Models;
using Domain.Options;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;
using BookServiceException = PracticalWork.Library.Exceptions.BookServiceException;

namespace PracticalWork.Library.Application.Services;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly ICursorPaginationService<Book> _bookPaginationService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICacheService _cacheService;
    private readonly IRabbitMqPublisher _publisher;
    private readonly string _booksCacheVersionKey;
    private readonly string _booksListCachePrefix;
    private readonly double _booksListCacheTtlInMinutes;
    private readonly string _coversBucketName;
    private readonly string _libraryExchangeName;
    private readonly string _bookCreateRoutingKey;
    private readonly string _bookArchiveRoutingKey;
    private readonly TimeProvider _timeProvider;

    public BookService(IBookRepository bookRepository, 
        ICursorPaginationService<Book> paginationService,
        IFileStorageService fileStorageService,
        ICacheService cacheService,
        IRabbitMqPublisher publisher,
        IOptionsMonitor<MinioOptions> minioOptions,
        IOptionsMonitor<RabbitOptions> rabbitOptions,
        IOptionsMonitor<RedisOptions> redisOptions, TimeProvider timeProvider)
    {
        _bookRepository = bookRepository;
        _bookPaginationService = paginationService;
        _fileStorageService = fileStorageService;
        _cacheService = cacheService;
        _publisher = publisher;
        _timeProvider = timeProvider;
        var minioOpt = minioOptions.CurrentValue;
        var redisOpt = redisOptions.CurrentValue;
        var rabbitOpt = rabbitOptions.CurrentValue;

        _booksCacheVersionKey = redisOpt.Books.VersionKey;
        _booksListCachePrefix = redisOpt.Books.BooksList.Prefix;
        _booksListCacheTtlInMinutes = redisOpt.Books.BooksList.TtlInMinutes;
        _bookCreateRoutingKey = rabbitOpt.Library.BookCreate.RoutingKey;
        _bookArchiveRoutingKey = rabbitOpt.Library.BookArchive.RoutingKey;
        _libraryExchangeName = rabbitOpt.Library.ExchangeName;
        _coversBucketName = minioOpt.CoversBucketName;
    }

    public async Task<Guid> CreateBook(Book book, CancellationToken cancellationToken)
    {
        book.Status = BookStatus.Available;
        try
        {
            var bookId = await _bookRepository.CreateBook(book, cancellationToken);
            var message = new BookCreatedEvent(
                bookId, book.Title,
                book.Category.ToString(),
                book.Authors.ToArray(),
                book.Year);
            await _publisher.PublishAsync(_libraryExchangeName,_bookCreateRoutingKey,
                message, cancellationToken);
            await _cacheService.InvalidateCache(_booksCacheVersionKey);
            return bookId;
            
        }
        catch (Exception ex)
        {
            throw new BookServiceException("Ошибка создание книги!", ex);
        }
    }

    public async Task UpdateBook(Guid id, Book updatedBook, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetBookById(id, cancellationToken);
        if (book.IsArchived || book.Status == BookStatus.Archived)
        {
            throw new BookServiceException("Книга в архиве");
        }

        book.Update(updatedBook.Title, updatedBook.Description,
            updatedBook.Year, updatedBook.Authors);
        await _bookRepository.UpdateBook(id, book, cancellationToken);
        await _cacheService.InvalidateCache(_booksCacheVersionKey);
    }

    public async Task<BookArchive> ArchiveBook(Guid id, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetBookById(id, cancellationToken);
        book.Archive();
        await _bookRepository.UpdateBook(id, book, cancellationToken);
        await _cacheService.InvalidateCache(_booksCacheVersionKey);
        var response = new BookArchive
        {
            Id = id,
            Title = book.Title,
            ArchivedAt = _timeProvider.GetUtcNow().UtcDateTime
        };
        var message = new BookArchivedEvent(id, book.Title, 
            "Вызван метод архивации книги", response.ArchivedAt);
        await _publisher.PublishAsync(
            _libraryExchangeName, 
           _bookArchiveRoutingKey, 
            message, cancellationToken);
        
        return response;
    }

    public async Task<CursorPaginationResponse<Book>> GetBooksPage(CursorPaginationRequest request, 
        BookStatus? status, BookCategory? category, string author, CancellationToken cancellationToken)
    {
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_booksCacheVersionKey);
        var prms = new { category, author, status, request };
        var cacheKey = _cacheService.GenerateCacheKey(_booksListCachePrefix, cacheVersion, prms);
        var cachedResult = await _cacheService.GetAsync<CursorPaginationResponse<Book>>(cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var page = await _bookRepository.GetBooksPageFilteringByFields(
            request, status, category, author, cancellationToken);
        var paginationResponse = _bookPaginationService.ToCursorPageResponse(page, request);

        await _cacheService.SetAsync(
            cacheKey,
            paginationResponse,
            TimeSpan.FromMinutes(_booksListCacheTtlInMinutes));

        return paginationResponse;
    }

    public async Task AddBookDetails(Guid bookId, string description, Stream coverImageStream, 
        string contentType, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetBookById(bookId, cancellationToken);
        var currentDate = _timeProvider.GetUtcNow().UtcDateTime;
        var fileName = $"book-covers/{currentDate.Year}/{currentDate.Month}/{bookId}";
        await _fileStorageService.UploadFileAsync(_coversBucketName,fileName, coverImageStream, contentType, cancellationToken);
        book.Description = description;
        book.CoverImagePath = fileName;
        await _bookRepository.UpdateBook(bookId, book, cancellationToken);
        await _cacheService.InvalidateCache(_booksCacheVersionKey);
    }
}