using System.ComponentModel;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Services;

public class LibraryService: ILibraryService
{
    private readonly IReaderRepository _readerRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IBorrowRepository _borrowRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICursorPaginationService<Book> _bookPaginationService;
    private readonly ICacheService _cacheService;
    private const string LibraryCacheVersionKey = "library:cache:version";
    private const string BooksCacheVersionKey = "books:cache:version";
    private const int PageCacheDurationMinutes = 5;
    
    public LibraryService(IReaderRepository readerRepository, 
        IBookRepository bookRepository,
        IBorrowRepository borrowRepository,
        IFileStorageService fileStorageService,
        ICursorPaginationService<Book> bookPaginationService,
        ICacheService cacheService)
    {
        _readerRepository = readerRepository;
        _bookRepository = bookRepository;
        _borrowRepository = borrowRepository;
        _fileStorageService = fileStorageService;
        _bookPaginationService = bookPaginationService;
        _cacheService = cacheService;
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
        await IncrementCacheVersion();
    }

    public async Task ReturnBook(Guid bookId, Guid readerId)
    {
        var (id, bookBorrow) = await _borrowRepository.GetBookBorrow(bookId, readerId);
        bookBorrow.ReturnBookBorrow();
        await _borrowRepository.ReturnBookBorrow(id, bookBorrow);
        await IncrementCacheVersion();
    }

    public async Task<(Guid bookId, Book book)> GetBookDetails(Guid bookId)
    {
        var book = await _bookRepository.GetBookById(bookId);
        if (book.CoverImagePath is not null)
        {
            book.CoverImagePath = await _fileStorageService.GetFileLinkAsync(book.CoverImagePath);
        }
        return (bookId, book);
    }

    public async Task<(Guid bookId, Book book)> GetBookDetails(string title)
    {
        var (id,book) = await _bookRepository.GetBookByTitle(title);
        book.CoverImagePath = await _fileStorageService.GetFileLinkAsync(book.CoverImagePath);
        return (id, book);
    }

    public async Task<CursorPaginationResponse<Book>> GetNonArchivedBooksPage(CursorPaginationRequest request)
    {
        var cacheVersion = await GetCurrentCacheVersion();
        var cacheKey = GenerateBooksPageCacheKey(request, cacheVersion);
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
            TimeSpan.FromMinutes(PageCacheDurationMinutes));
            
        return cursorResponse;
    }
    
    private string GenerateBooksPageCacheKey(
        CursorPaginationRequest request,
        long cacheVersion)
    {
        var cursorPart = !string.IsNullOrEmpty(request.Cursor) ? $":cursor:{request.Cursor}" : "";
        return $"library:v{cacheVersion}:page:limit:{request.PageSize}{cursorPart}:forward:{request.Forward}";
    }
    private async Task<long> GetCurrentCacheVersion()
    {
        var version = await _cacheService.GetAsync<long>(LibraryCacheVersionKey);
        return version == 0 ? 1 : version;
    }
    private async Task IncrementCacheVersion()
    {
        var currentVersion = await GetCurrentCacheVersion();
        var newVersion = currentVersion + 1;
        var bookVersion = await _cacheService.GetAsync<long>(BooksCacheVersionKey);
        await _cacheService.SetAsync(LibraryCacheVersionKey, newVersion);
        await _cacheService.SetAsync(BooksCacheVersionKey, bookVersion + 1);
    }
}