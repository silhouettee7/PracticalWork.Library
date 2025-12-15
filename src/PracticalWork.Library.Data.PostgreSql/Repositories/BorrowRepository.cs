using Microsoft.EntityFrameworkCore;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Data.PostgreSql.Extensions;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Data.PostgreSql.Repositories;

public class BorrowRepository: IBorrowRepository
{
    private readonly AppDbContext _appDbContext;

    public BorrowRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task CreateBookBorrow(Guid bookId, Guid readerId, BookBorrow bookBorrow)
    {
        BookBorrowEntity entity = new()
        {
            BookId = bookId,
            ReaderId = readerId,
            BorrowDate = bookBorrow.BorrowDate,
            DueDate = bookBorrow.DueDate,
            Status = bookBorrow.Status,
        };
        _appDbContext.BookBorrows.Add(entity);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<(Guid id, BookBorrow bookBorrow)> GetBookBorrow(Guid bookId, Guid readerId)
    {
        var entity = await _appDbContext.BookBorrows
            .Include(b => b.Book)
            .Where(b => b.Status == BookIssueStatus.Issued)
            .SingleOrDefaultAsync(b => b.BookId == bookId && b.ReaderId == readerId)
            ?? throw new EntityNotFoundException($"Выдача книги:{bookId} у читателя:{readerId} не обнаружена");
        return (entity.Id, entity.ToBookBorrow());
    }

    public async Task ReturnBookBorrow(Guid bookBorrowId, BookBorrow bookBorrow)
    {
        var entity = await _appDbContext.BookBorrows
            .Include(b => b.Book)
            .SingleOrDefaultAsync(b => b.Id == bookBorrowId)
            ?? throw new EntityNotFoundException($"Выдача книги не обнаружена, id:{bookBorrowId}");
        entity.Status = bookBorrow.Status;
        entity.ReturnDate = bookBorrow.ReturnDate;
        entity.Book.Status = bookBorrow.Book.Status;
        _appDbContext.BookBorrows.Update(entity);
        await _appDbContext.SaveChangesAsync();
    }
}