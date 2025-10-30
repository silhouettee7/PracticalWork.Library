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
    private readonly IBookBorrowRepository _bookBorrowRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICursorPaginationService<Book> _bookPaginationService;
    private readonly ICacheService _cacheService;
    private const string LibraryCacheVersionKey = "library:cache:version";
    private const int PageCacheDurationMinutes = 5;
    
    public LibraryService(IReaderRepository readerRepository, 
        IBookRepository bookRepository,
        IBookBorrowRepository bookBorrowRepository,
        IFileStorageService fileStorageService,
        ICursorPaginationService<Book> bookPaginationService,
        ICacheService cacheService)
    {
        _readerRepository = readerRepository;
        _bookRepository = bookRepository;
        _bookBorrowRepository = bookBorrowRepository;
        _fileStorageService = fileStorageService;
        _bookPaginationService = bookPaginationService;
        _cacheService = cacheService;
    }
    
    public async Task BorrowBook(Guid bookId, Guid readerId)
    {
        try
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
            await _bookBorrowRepository.CreateBookBorrow(bookId, readerId, bookBorrow);
            await IncrementCacheVersion();
        }
        catch (InvalidOperationException ex)
        {
            throw new ReaderServiceException("Обнаружены не уникальные книги или карточки", ex);
        }
        catch (NullReferenceException ex)
        {
            throw new ReaderServiceException("Книга или карточка не обнаружены", ex);
        }
        catch (Exception ex)
        {
            throw new LibraryServiceException("Не удалось выдать книгу", ex);
        }
    }

    public async Task ReturnBook(Guid bookId, Guid readerId)
    {
        try
        {
            var (id, bookBorrow) = await _bookBorrowRepository.GetBookBorrow(bookId, readerId);
            if (bookBorrow.Status != BookIssueStatus.Issued)
            {
                throw new LibraryServiceException("Книга уже возвращена");
            }
            bookBorrow.ReturnBookBorrow();
            await _bookBorrowRepository.UpdateReturnedBookBorrow(id, bookBorrow);
            await IncrementCacheVersion();
        }
        catch (InvalidOperationException ex)
        {
            throw new ReaderServiceException("Обнаружены не уникальные выдачи книг", ex);
        }
        catch (NullReferenceException ex)
        {
            throw new ReaderServiceException("Выдача книги не обнаружены", ex);
        }
        catch (Exception ex)
        {
            throw new LibraryServiceException("Не удалось вернуть книгу",ex);
        }
        
    }

    public async Task<(Guid bookId, Book book)> GetBookDetails(Guid bookId)
    {
        try
        {
            var book = await _bookRepository.GetBookById(bookId);
            book.CoverImagePath = await _fileStorageService.GetFileUrlAsync(book.CoverImagePath);
            return (bookId, book);
        }
        catch (InvalidOperationException ex)
        {
            throw new ReaderServiceException("Обнаружены не уникальные книги", ex);
        }
        catch (NullReferenceException ex)
        {
            throw new ReaderServiceException("Книга не обнаружена", ex);
        }
        catch (Exception ex)
        {
            throw new LibraryServiceException("Не удалось получить информацию о книге",ex);
        }
    }

    public async Task<(Guid bookId, Book book)> GetBookDetails(string title)
    {
        try
        {
            var (id,book) = await _bookRepository.GetBookByTitle(title);
            book.CoverImagePath = await _fileStorageService.GetFileUrlAsync(book.CoverImagePath);
            return (id, book);
        }
        catch (NullReferenceException ex)
        {
            throw new ReaderServiceException("Книга не обнаружена", ex);
        }
        catch (Exception ex)
        {
            throw new LibraryServiceException("Не удалось получить информацию о книге",ex);
        }
        
    }

    public async Task<CursorPaginationResponse<Book>> GetNonArchivedBooksPage(CursorPaginationRequest request)
    {
        try
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
        catch (Exception ex)
        {
            throw new LibraryServiceException("Не удалось получить страницу с книгами", ex);
        }
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
        await _cacheService.SetAsync(LibraryCacheVersionKey, newVersion);
    }
}