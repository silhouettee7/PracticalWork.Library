using Microsoft.EntityFrameworkCore;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Data.PostgreSql.Extensions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Data.PostgreSql.Repositories;

public class BookBorrowRepository: IBookBorrowRepository
{
    private readonly AppDbContext _appDbContext;

    public BookBorrowRepository(AppDbContext appDbContext)
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
            .SingleOrDefaultAsync(b => b.BookId == bookId && b.ReaderId == readerId);
        return (entity.Id, entity.ToBookBorrow());
    }

    public async Task UpdateReturnedBookBorrow(Guid bookBorrowId, BookBorrow bookBorrow)
    {
        var entity = await _appDbContext.BookBorrows
            .Include(b => b.Book)
            .SingleOrDefaultAsync(b => b.Id == bookBorrowId);
        entity.Status = bookBorrow.Status;
        entity.ReturnDate = bookBorrow.ReturnDate;
        entity.Book.Status = bookBorrow.Book.Status;
        _appDbContext.BookBorrows.Update(entity);
        await _appDbContext.SaveChangesAsync();
    }
}