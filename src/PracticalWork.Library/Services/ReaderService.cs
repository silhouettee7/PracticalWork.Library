using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Services;

public class ReaderService: IReaderService
{
    private readonly IReaderRepository _readerRepository;
    public ReaderService(IReaderRepository repository)
    {
        _readerRepository = repository;
    }
    public async Task<Guid> CreateReader(Reader reader)
    {
        if (await _readerRepository.IsExistReader(reader.PhoneNumber))
        {
            throw new ReaderServiceException("Phone number is not unique");
        }
        reader.IsActive = true;
        var id = await _readerRepository.CreateReader(reader);
        return id;
    }

    public async Task ExtendExpiryDate(Guid id, DateOnly date)
    {
        var reader = await _readerRepository.GetReader(id);
        if (!reader.IsActive)
        {
            throw new ReaderServiceException("Карточка неактивна");
        }

        if (reader.ExpiryDate >= date)
        {
            throw new ReaderServiceException("Необходимо продлить карточку на будущую дату");
        }
        reader.ExpiryDate = date;
        await _readerRepository.UpdateReader(id, reader);
    }

    public async Task<(bool borrowBooksExist, IReadOnlyList<Book> borrowBooks)> CloseReader(Guid id)
    {
        var readerWithBorrowBooks = await _readerRepository.GetReaderWithBorrowBooks(id);
        var borrowBooksExist = readerWithBorrowBooks.BorrowBooks.Any();
        if (borrowBooksExist)
        {
            return (true, readerWithBorrowBooks.BorrowBooks);
        }
        readerWithBorrowBooks.IsActive = false;
        readerWithBorrowBooks.ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await _readerRepository.UpdateReader(id, readerWithBorrowBooks);
        return (false, readerWithBorrowBooks.BorrowBooks);
    }

    public async Task<IReadOnlyList<BorrowedBook>> GetAllBorrowBooks(Guid readerId)
    {
        var result = await _readerRepository
            .GetReadersBorrowBooks(readerId);
        return result;
    }
}