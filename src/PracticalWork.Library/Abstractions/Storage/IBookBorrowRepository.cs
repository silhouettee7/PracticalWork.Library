using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

public interface IBookBorrowRepository
{
    Task CreateBookBorrow(Guid bookId, Guid readerId, BookBorrow bookBorrow);
    Task<(Guid id, BookBorrow bookBorrow)> GetBookBorrow(Guid bookId, Guid readerId);
    Task UpdateReturnedBookBorrow(Guid bookBorrowId, BookBorrow bookBorrow);
}