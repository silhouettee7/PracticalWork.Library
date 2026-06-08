using Domain.Exceptions;
using Domain.Extensions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Data.PostgreSql.Extensions;
using PracticalWork.Library.Dtos;
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

    /// <inheritdoc />
    public async Task<Guid> CreateBook(Book book, CancellationToken cancellationToken)
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
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    /// <inheritdoc />
    public async Task<Book> GetBookById(Guid id, CancellationToken cancellationToken)
    {
        var bookEntity = await _appDbContext.Books
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken: cancellationToken)
            ?? throw new EntityNotFoundException($"Книга с id:{id} не найдена");
        return bookEntity.ToBook();
    }

    /// <inheritdoc />
    public async Task<(Guid id, Book book)> GetBookByTitle(string title, CancellationToken cancellationToken)
    {
        var bookEntity = await _appDbContext.Books
            .FirstOrDefaultAsync(b => b.Title == title, cancellationToken: cancellationToken)
            ?? throw new EntityNotFoundException($"Книга с названием:{title} не найдена");
        return (bookEntity.Id,bookEntity.ToBook());
    }

    /// <inheritdoc />
    public async Task UpdateBook(Guid id, Book book, CancellationToken cancellationToken)
    {
        var entity = await _appDbContext.Books.SingleOrDefaultAsync(b => b.Id == id, cancellationToken: cancellationToken) 
                     ?? throw new EntityNotFoundException($"Книга с id:{id} не найдена");
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
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Book>> GetBooksPageFilteringByFields(CursorPaginationRequest request,
        BookStatus? status, BookCategory? category, string author, CancellationToken cancellationToken)
    {
        IQueryable<AbstractBookEntity> query = category switch
        {
            BookCategory.ScientificBook => _appDbContext.ScientificBooks,
            BookCategory.EducationalBook => _appDbContext.EducationalBooks,
            BookCategory.FictionBook => _appDbContext.FictionBooks,
            _ => _appDbContext.Books
        };
        var entities = query
            .Where(b => !status.HasValue || b.Status == status)
            .Where(b => !category.HasValue || b.Category == category)
            .Where(b => string.IsNullOrWhiteSpace(author) || b.Authors.Contains(author))
            .CursorPage(request)    
            .Select(e => e.ToBook());
        
        return await entities.ToListAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Book>> GetNonArchivedBooksPageWithIssuanceRecords(
        CursorPaginationRequest request, CancellationToken cancellationToken)
    {
        var entities = _appDbContext.Books
            .Where(b => b.Status != BookStatus.Archived)
            .Include(b => b.IssuanceRecords)
            .CursorPage(request)
            .Select(e => e.ToBook());
        
        return await entities.ToListAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<AvailableOldBookDto>> GetAvailableOldBooksPage(
        DateOnly borrowDateTo, CursorPaginationRequest request, 
        CancellationToken cancellationToken)
    {
        return await _appDbContext.Books
            .Include(b => b.IssuanceRecords)
            .Where(b => b.Status == BookStatus.Available &&
                        (b.IssuanceRecords.Count == 0 ||
                        b.IssuanceRecords.All(r => r.BorrowDate < borrowDateTo)))
            .CursorPage(request)
            .Select(b => new AvailableOldBookDto
            {
                Id = b.Id,
                Title = b.Title,
            })
            .ToListAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetAddedBooksCount(DateTime startDate, DateTime endDate, 
        CancellationToken cancellationToken)
    {
        return await _appDbContext.Books
            .Where(b => b.CreatedAt >= startDate && b.CreatedAt < endDate)
            .CountAsync(cancellationToken: cancellationToken);
    }
}