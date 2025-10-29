using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

public interface IReaderService
{
    Task<Guid> CreateReader(Reader reader);
    Task ExtendExpiryDate(Guid id, DateOnly date);
    Task<(bool borrowBooksExist, IReadOnlyList<Book> borrowBooks)> CloseReader(Guid id);
    Task<IReadOnlyList<BorrowedBook>> GetAllBorrowBooks(Guid readerId);
}