using Microsoft.Extensions.Configuration;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Services;

public class ReaderService: IReaderService
{
    private readonly IReaderRepository _readerRepository;
    private readonly ICacheService _cacheService;
    private readonly string _readerBooksPrefix;
    private readonly double _readerBooksTtlInMinutes;
    private readonly string _readersCacheVersion;
    public ReaderService(IReaderRepository repository,
        IConfiguration configuration,
        ICacheService cacheService)
    {
        _readerRepository = repository;
        _cacheService = cacheService;
        var section = configuration.GetSection("App:Redis:Readers");
        _readersCacheVersion = section["VersionKey"];
        _readerBooksPrefix = section["ReaderBooks:Prefix"];
        _readerBooksTtlInMinutes = section.GetValue<double>("ReaderBooks:TtlInMinutes");
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
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_readersCacheVersion);
        var cacheKey = _cacheService.GenerateCacheKey(_readerBooksPrefix, cacheVersion, null);
        var cachedResult = await _cacheService.GetAsync<IReadOnlyList<BorrowedBook>>(cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }
        var result = await _readerRepository
            .GetReadersBorrowBooks(readerId);
        if (!result.isActive)
        {
            throw new ReaderServiceException("Карточка неактивна");
        }
        await _cacheService.SetAsync(
            cacheKey, 
            result.books, 
            TimeSpan.FromMinutes(_readerBooksTtlInMinutes));
        return result.books;
    }
}