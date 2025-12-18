using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Events;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Services;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly ICursorPaginationService<Book> _bookPaginationService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICacheService _cacheService;
    private readonly IRabbitMQPublisher _publisher;
    private readonly MinioOptions _minioOptions;
    private readonly string _cacheVersion;
    private readonly string _booksListPrefix;
    private readonly double _cacheTtlInMinutes;
    private readonly IConfigurationSection _rabbitLibrarySection;
    private readonly string _exchangeName;

    public BookService(IBookRepository bookRepository, 
        ICursorPaginationService<Book> paginationService,
        IFileStorageService fileStorageService,
        ICacheService cacheService,
        IRabbitMQPublisher publisher,
        IConfiguration configuration,
        IOptionsMonitor<MinioOptions> minioOptions)
    {
        _bookRepository = bookRepository;
        _bookPaginationService = paginationService;
        _fileStorageService = fileStorageService;
        _cacheService = cacheService;
        _publisher = publisher;
        _minioOptions = minioOptions.CurrentValue;
        var section = configuration.GetSection("App:Redis:Books");
        _cacheVersion = section["VersionKey"];
        _booksListPrefix = section["BooksList:Prefix"];
        _cacheTtlInMinutes = section.GetValue<double>("BooksList:TtlInMinutes");
        _rabbitLibrarySection = configuration.GetSection("App:RabbitMQ:Library");
        _exchangeName = _rabbitLibrarySection["ExchangeName"];
    }

    public async Task<Guid> CreateBook(Book book)
    {
        book.Status = BookStatus.Available;
        try
        {
            var bookId = await _bookRepository.CreateBook(book);
            var message = new BookCreatedEvent(
                bookId, book.Title,
                book.Category.ToString(),
                book.Authors.ToArray(),
                book.Year);
            await _publisher.PublishAsync(_exchangeName,_rabbitLibrarySection["BookCreate:RoutingKey"],message);
            await _cacheService.InvalidateCache(_cacheVersion);
            return bookId;
            
        }
        catch (Exception ex)
        {
            throw new BookServiceException("Ошибка создание книги!", ex);
        }
    }

    public async Task UpdateBook(Guid id, Book updatedBook)
    {
        var book = await _bookRepository.GetBookById(id);
        if (book.IsArchived)
        {
            throw new BookServiceException("Книга в архиве");
        }

        book.Update(updatedBook.Title, updatedBook.Description,
            updatedBook.Year, updatedBook.Authors);
        await _bookRepository.UpdateBook(id, book);
        await _cacheService.InvalidateCache(_cacheVersion);
    }

    public async Task<BookArchive> ArchiveBook(Guid id)
    {
        var book = await _bookRepository.GetBookById(id);
        book.Archive();
        await _bookRepository.UpdateBook(id, book);
        var response = new BookArchive
        {
            Id = id,
            Title = book.Title,
            ArchivedAt = DateTime.UtcNow
        };
        var message = new BookArchivedEvent(id, book.Title, 
            "Вызван метод архивации книги", response.ArchivedAt);
        await _publisher.PublishAsync(
            _exchangeName, 
            _rabbitLibrarySection["BookArchive:RoutingKey"], 
            message);
        await _cacheService.InvalidateCache(_cacheVersion);
        return response;
    }

    public async Task<CursorPaginationResponse<Book>> GetBooksPage(CursorPaginationRequest request, BookStatus? status, BookCategory? category, string author)
    {
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_cacheVersion);
        var prms = new { category, author, status, request };
        var cacheKey = _cacheService.GenerateCacheKey(_booksListPrefix, cacheVersion, prms);
        var cachedResult = await _cacheService.GetAsync<CursorPaginationResponse<Book>>(cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var page = await _bookRepository.GetBooksPageFilteringByFields(request, status, category, author);
        var paginationResponse = _bookPaginationService.ToCursorPageResponse(page, request);

        await _cacheService.SetAsync(
            cacheKey,
            paginationResponse,
            TimeSpan.FromMinutes(_cacheTtlInMinutes));

        return paginationResponse;
    }

    public async Task AddBookDetails(Guid bookId, string description, Stream coverImageStream, string contentType)
    {
        var book = await _bookRepository.GetBookById(bookId);
        var currentDate = DateTime.UtcNow;
        var fileName = $"book-covers/{currentDate.Year}/{currentDate.Month}/{bookId}";
        await _fileStorageService.UploadFileAsync(_minioOptions.CoversBucketName,fileName, coverImageStream, contentType);
        book.Description = description;
        book.CoverImagePath = fileName;
        await _bookRepository.UpdateBook(bookId, book);
        await _cacheService.InvalidateCache(_cacheVersion);
    }
}