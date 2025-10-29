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
        try
        {
            if (!await _readerRepository.IsExistReaderAsync(reader.PhoneNumber))
            {
                throw new ReaderServiceException("Phone number is not unique");
            }
            reader.IsActive = true;
            var id = await _readerRepository.CreateReaderAsync(reader);
            return id;
        }
        catch (Exception ex)
        {
            throw new ReaderServiceException("Не удалось создать карточку читателя", ex);
        }
    }

    public async Task ExtendExpiryDate(Guid id, DateOnly date)
    {
        try
        {
            var reader = await _readerRepository.GetReaderAsync(id);
            if (!reader.IsActive)
            {
                throw new ReaderServiceException("Карточка неактивна");
            }

            if (reader.ExpiryDate >= date)
            {
                throw new ReaderServiceException("Необходимо продлить карточку на будущую дату");
            }
            reader.ExpiryDate = date;
            await _readerRepository.UpdateReaderAsync(id, reader);
        }
        catch (InvalidOperationException ex)
        {
            throw new ReaderServiceException("Обнаружены не уникальные карточки", ex);
        }
        catch (NullReferenceException ex)
        {
            throw new ReaderServiceException("Карточка не обнаружена", ex);
        }
        catch (Exception ex)
        {
            throw new ReaderServiceException("Не удалось продлить срок действия карточки", ex);
        }
    }

    public async Task<(bool borrowBooksExist, IReadOnlyList<Book> borrowBooks)> CloseReader(Guid id)
    {
        try
        {
            var readerWithBorrowBooks = await _readerRepository.GetReaderWithBorrowBooksAsync(id);
            var borrowBooksExist = readerWithBorrowBooks.BorrowBooks.Any();
            if (borrowBooksExist)
            {
                return (true, readerWithBorrowBooks.BorrowBooks);
            }
            readerWithBorrowBooks.IsActive = false;
            readerWithBorrowBooks.ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow);
            await _readerRepository.UpdateReaderAsync(id, readerWithBorrowBooks);
            return (false, readerWithBorrowBooks.BorrowBooks);
        }
        catch (InvalidOperationException ex)
        {
            throw new ReaderServiceException("Обнаружены не уникальные карточки", ex);
        }
        catch (NullReferenceException ex)
        {
            throw new ReaderServiceException("Карточка не обнаружена", ex);
        }
        catch (Exception ex)
        {
            throw new ReaderServiceException("Не удалось закрыть карточку",ex);
        }
    }

    public async Task<IReadOnlyList<BorrowedBook>> GetAllBorrowBooks(Guid readerId)
    {
        try
        {
            var result = await _readerRepository
                .GetReadersBorrowBooksAsync(readerId);
            return result;
        }
        catch (InvalidOperationException ex)
        {
            throw new ReaderServiceException("Обнаружены не уникальные карточки", ex);
        }
        catch (NullReferenceException ex)
        {
            throw new ReaderServiceException("Карточка не обнаружена", ex);
        }
        catch (Exception ex)
        {
            throw new ReaderServiceException("Не удалось получить информацию о взятых книгах",ex);
        }
    }
}