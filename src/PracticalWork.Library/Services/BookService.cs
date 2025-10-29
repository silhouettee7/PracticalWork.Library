using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Services;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly ICursorPaginationService<Book> _bookPaginationService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICacheService _cacheService;
    private const string BooksCacheVersionKey = "books:cache:version";
    private const int PageCacheDurationMinutes = 10;

    public BookService(IBookRepository bookRepository, 
        ICursorPaginationService<Book> paginationService,
        IFileStorageService fileStorageService,
        ICacheService cacheService)
    {
        _bookRepository = bookRepository;
        _bookPaginationService = paginationService;
        _fileStorageService = fileStorageService;
        _cacheService = cacheService;
    }

    public async Task<Guid> CreateBook(Book book)
    {
        book.Status = BookStatus.Available;
        try
        {
            var bookId = await _bookRepository.CreateBook(book);
            await IncrementCacheVersion();
            return bookId;
            
        }
        catch (Exception ex)
        {
            throw new BookServiceException("Ошибка создание книги!", ex);
        }
    }

    public async Task UpdateBook(Guid id, Book updatedBook)
    {
        try
        {
            var book = await _bookRepository.GetBook(id) ?? throw new BookNotFoundException("Книга не найдена");
            if (book.IsArchived)
            {
                throw new BookServiceException("Книга в архиве");
            }

            book.Update(updatedBook.Title, updatedBook.Description,
                updatedBook.Year, updatedBook.Authors);
            await _bookRepository.UpdateBook(book);
            await IncrementCacheVersion();
        }
        catch (Exception ex)
        {
            throw new BookServiceException("Не удалось обновить книгу", ex);
        }
    }

    public async Task<BookArchive> ArchiveBook(Guid id)
    {
        try
        {
            var book = await _bookRepository.GetBook(id) ?? throw new BookNotFoundException("Книга не найдена");
            book.Archive();
            await _bookRepository.UpdateBook(book);
            await IncrementCacheVersion();
            var response = new BookArchive
            {
                Id = id,
                Title = book.Title,
                ArchivedAt = DateTime.UtcNow
            };
            return response;
        }
        catch (Exception ex)
        {
            throw new BookServiceException("Не удалось заархивировать книгу", ex);
        }
    }

    public async Task<CursorPaginationResponse<Book>> GetBooksPage(CursorPaginationRequest request, BookStatus status, BookCategory category, string author)
    {
        var cacheVersion = await GetCurrentCacheVersion();
        var cacheKey = GenerateBooksPageCacheKey(request, status, category, author, cacheVersion);
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
            TimeSpan.FromMinutes(PageCacheDurationMinutes));
        
        return paginationResponse;
    }

    public async Task AddBookDetails(Guid bookId, string description, Stream coverImageStream, string contentType)
    {
        try
        {
            var book = await _bookRepository.GetBook(bookId) ?? throw new BookNotFoundException("Книга не найдена");
            var fileName = $"{bookId}_coverImage";
            await _fileStorageService.UploadFileAsync(fileName, coverImageStream, contentType);
            book.Description = description;
            book.CoverImagePath = fileName;
            await _bookRepository.UpdateBook(book);
            await IncrementCacheVersion();
        }
        catch (Exception ex)
        {
            throw new BookServiceException("Не удалось добавить детали книги", ex);
        }
    }
    
    private string GenerateBooksPageCacheKey(
        CursorPaginationRequest request, 
        BookStatus status, 
        BookCategory category, 
        string author, 
        long cacheVersion)
    {
        var statusPart = $":status:{status}";
        var categoryPart = $":category:{category}";
        var authorPart = $":author:{author.ToLower()}";
        var cursorPart = !string.IsNullOrEmpty(request.Cursor) ? $":cursor:{request.Cursor}" : "";
        
        return $"books:v{cacheVersion}:page:limit:{request.PageSize}{statusPart}{categoryPart}{authorPart}{cursorPart}";
    }
    private async Task<long> GetCurrentCacheVersion()
    {
        var version = await _cacheService.GetAsync<long>(BooksCacheVersionKey);
        return version == 0 ? 1 : version;
    }
    private async Task IncrementCacheVersion()
    {
        var currentVersion = await GetCurrentCacheVersion();
        var newVersion = currentVersion + 1;
        await _cacheService.SetAsync(BooksCacheVersionKey, newVersion);
    }
}