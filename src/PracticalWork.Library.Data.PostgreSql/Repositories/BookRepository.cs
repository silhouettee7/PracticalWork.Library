using Microsoft.EntityFrameworkCore;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Data.PostgreSql.Extensions;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Data.PostgreSql.Repositories;

public sealed class BookRepository : IBookRepository
{
    private readonly AppDbContext _appDbContext;

    public BookRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Guid> CreateBook(Book book)
    {
        AbstractBookEntity entity = book.Category switch
        {
            BookCategory.ScientificBook => new ScientificBookEntity(),
            BookCategory.EducationalBook => new EducationalBookEntity(),
            BookCategory.FictionBook => new FictionBookEntity(),
            _ => throw new ArgumentException($"Неподдерживаемая категория книги: {book.Category}", nameof(book.Category))
        };

        entity.Title = book.Title;
        entity.Description = book.Description;
        entity.Year = book.Year;
        entity.Authors = book.Authors;
        entity.Status = book.Status;
        entity.Category = book.Category;
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        
        _appDbContext.Add(entity);
        await _appDbContext.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<Book> GetBookById(Guid id)
    {
        var bookEntity = await _appDbContext.Books
            .SingleOrDefaultAsync(b => b.Id == id);
        return bookEntity.ToBook();
    }

    public async Task<(Guid id, Book book)> GetBookByTitle(string title)
    {
        var bookEntity = await _appDbContext.Books
            .FirstOrDefaultAsync(b => b.Title == title);
        return (bookEntity.Id,bookEntity.ToBook());
    }

    public async Task UpdateBook(Guid id, Book book)
    {
        var entity = await _appDbContext.Books.FindAsync(id);
        entity.Title = book.Title;
        entity.Description = book.Description;
        entity.Year = book.Year;
        entity.Authors = book.Authors;
        entity.Status = book.Status;
        entity.UpdatedAt = DateTime.UtcNow;
        if (book.CoverImagePath != null)
        {
            entity.CoverImagePath = book.CoverImagePath;
        }
        _appDbContext.Update(entity);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Book>> GetBooksPageFilteringByFields(
        CursorPaginationRequest request, BookStatus status, BookCategory category, string author)
    {
        IQueryable<AbstractBookEntity> query = category switch
        {
            BookCategory.ScientificBook => _appDbContext.ScientificBooks,
            BookCategory.EducationalBook => _appDbContext.EducationalBooks,
            BookCategory.FictionBook => _appDbContext.FictionBooks,
            _ => _appDbContext.Books
        };
        var entities = query
            .Where(b => b.Status == status && b.Authors.Contains(author))
            .CursorPage(request)
            .Select(e => e.ToBook());
        
        return await entities.ToListAsync();
    }

    public async Task<IReadOnlyList<Book>> GetNonArchivedBooksPageWithIssuanceRecords(
        CursorPaginationRequest request)
    {
        var entities = _appDbContext.Books
            .Where(b => b.Status != BookStatus.Archived)
            .Include(b => b.IssuanceRecords)
            .CursorPage(request)
            .Select(e => e.ToBook());
        
        return await entities.ToListAsync();
    }
}