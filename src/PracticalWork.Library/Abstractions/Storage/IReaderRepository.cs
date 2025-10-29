using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

public interface IReaderRepository
{
    Task<Guid> CreateReaderAsync(Reader reader);
    Task<bool> IsExistReaderAsync(string phone);
    Task<Reader> GetReaderAsync(Guid id);
    Task UpdateReaderAsync(Guid id, Reader reader);
    Task<Reader> GetReaderWithBorrowBooksAsync(Guid id);
    Task<IReadOnlyList<BorrowedBook>> GetReadersBorrowBooksAsync(Guid id);
}