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
        
        _appDbContext.Add(entity);
        await _appDbContext.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<Book> GetBook(Guid id)
    {
        var bookEntity = await _appDbContext.Books
            .SingleOrDefaultAsync(b => b.Id == id);
        return new Book
        {
            Id = bookEntity.Id,
            Authors = bookEntity.Authors,
            Description = bookEntity.Description,
            CoverImagePath = bookEntity.CoverImagePath,
            Status = bookEntity.Status,
            Title = bookEntity.Title,
            Year = bookEntity.Year,
            IsArchived = bookEntity.Status == BookStatus.Archived,
            Category = bookEntity.Category,
        };
    }

    public async Task UpdateBook(Book book)
    {
        var entity = await _appDbContext.Books.FindAsync(book.Id) ?? throw new Exception("Не удалось найти книгу в базе данных");
        entity.Title = book.Title;
        entity.Description = book.Description;
        entity.Year = book.Year;
        entity.Authors = book.Authors;
        entity.Status = book.Status;
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
            .Select(e => new Book
            {
                Id = e.Id,
                Authors = e.Authors,
                Category = e.Category,
                CoverImagePath = e.CoverImagePath,
                Description = e.Description,
                Status = e.Status,
                Title = e.Title,
                Year = e.Year,
                IsArchived = e.Status == BookStatus.Archived
            });
        
        return await entities.ToListAsync();
    }
}