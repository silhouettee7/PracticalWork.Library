using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

public interface IReaderRepository
{
    Task<Guid> CreateReader(Reader reader);
    Task<bool> IsExistReader(string phone);
    Task<Reader> GetReader(Guid id);
    Task UpdateReader(Guid id, Reader reader);
    Task<Reader> GetReaderWithBorrowBooks(Guid id);
    Task<IReadOnlyList<BorrowedBook>> GetReadersBorrowBooks(Guid id);
}