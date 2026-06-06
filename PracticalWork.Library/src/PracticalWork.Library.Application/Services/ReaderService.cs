using Domain.Abstractions.MessageBroker;
using Domain.Abstractions.Services;
using Domain.Events;
using Domain.Options;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Models;
using ReaderServiceException = PracticalWork.Library.Exceptions.ReaderServiceException;

namespace PracticalWork.Library.Application.Services;

public class ReaderService: IReaderService
{
    private readonly IReaderRepository _readerRepository;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ICacheService _cacheService;
    private readonly string _readerBooksCachePrefix;
    private readonly double _readerBooksCacheTtlInMinutes;
    private readonly string _readersCacheVersionKey;
    private readonly string _readerCreateRoutingKey;
    private readonly string _readerCloseRoutingKey;
    private readonly string _libraryExchangeName;
    private readonly TimeProvider _timeProvider;
    
    public ReaderService(IReaderRepository repository,
        ICacheService cacheService,
        IRabbitMqPublisher publisher,
        IOptionsMonitor<RabbitOptions> rabbitOptions,
        IOptionsMonitor<RedisOptions> redisOptions, TimeProvider timeProvider)
    {
        _readerRepository = repository;
        _cacheService = cacheService;
        _publisher = publisher;
        _timeProvider = timeProvider;
        var redisOpt = redisOptions.CurrentValue;
        var rabbitOpt = rabbitOptions.CurrentValue;
    
        _readersCacheVersionKey = redisOpt.Readers.VersionKey;
        _readerBooksCachePrefix = redisOpt.Readers.ReaderBooks.Prefix;
        _readerBooksCacheTtlInMinutes = redisOpt.Readers.ReaderBooks.TtlInMinutes;
        _libraryExchangeName = rabbitOpt.Library.ExchangeName;
        _readerCreateRoutingKey = rabbitOpt.Library.ReaderCreate.RoutingKey;
        _readerCloseRoutingKey = rabbitOpt.Library.ReaderClose.RoutingKey;
    }
    public async Task<Guid> CreateReader(Reader reader, CancellationToken cancellationToken)
    {
        if (await _readerRepository.IsExistReader(reader.PhoneNumber, cancellationToken))
        {
            throw new ReaderServiceException("Phone number is not unique");
        }
        reader.IsActive = true;
        var id = await _readerRepository.CreateReader(reader, cancellationToken);
        var message = new ReaderCreatedEvent(id, reader.FullName,
            reader.PhoneNumber, reader.ExpiryDate, _timeProvider.GetUtcNow().UtcDateTime);
        await _publisher.PublishAsync(
            _libraryExchangeName, 
            _readerCreateRoutingKey, 
            message, cancellationToken);
        return id;
    }

    public async Task ExtendExpiryDate(Guid id, DateOnly date, CancellationToken cancellationToken)
    {
        var reader = await _readerRepository.GetReader(id, cancellationToken);
        if (!reader.IsActive)
        {
            throw new ReaderServiceException("Карточка неактивна");
        }

        if (reader.ExpiryDate >= date)
        {
            throw new ReaderServiceException("Необходимо продлить карточку на будущую дату");
        }
        reader.ExpiryDate = date;
        await _readerRepository.UpdateReader(id, reader, cancellationToken);
    }

    public async Task<(bool borrowBooksExist, IReadOnlyList<Book> borrowBooks)> CloseReader(
        Guid id, CancellationToken cancellationToken)
    {
        var readerWithBorrowBooks = await _readerRepository.GetReaderWithBorrowBooks(id, cancellationToken);
        var borrowBooksExist = readerWithBorrowBooks.BorrowBooks.Any();
        if (borrowBooksExist)
        {
            return (true, readerWithBorrowBooks.BorrowBooks);
        }

        if (!readerWithBorrowBooks.IsActive)
        {
            throw new ReaderServiceException("Карточка уже закрыта");
        }
        readerWithBorrowBooks.IsActive = false;
        readerWithBorrowBooks.ExpiryDate = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        await _readerRepository.UpdateReader(id, readerWithBorrowBooks, cancellationToken);
        var message = new ReaderClosedEvent(id, readerWithBorrowBooks.FullName,
            _timeProvider.GetUtcNow().UtcDateTime, "Вызван метод закрытия карточки");
        await _publisher.PublishAsync(
            _libraryExchangeName, 
            _readerCloseRoutingKey, 
            message, cancellationToken);
        return (false, readerWithBorrowBooks.BorrowBooks);
    }

    public async Task<IReadOnlyList<BorrowedBook>> GetAllBorrowBooks(
        Guid readerId, CancellationToken cancellationToken)
    {
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_readersCacheVersionKey);
        var cacheKey = _cacheService.GenerateCacheKey(_readerBooksCachePrefix, cacheVersion, null);
        var cachedResult = await _cacheService.GetAsync<IReadOnlyList<BorrowedBook>>(cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }
        var result = await _readerRepository
            .GetReadersBorrowBooks(readerId, cancellationToken);
        if (!result.isActive)
        {
            throw new ReaderServiceException("Карточка неактивна");
        }
        await _cacheService.SetAsync(
            cacheKey, 
            result.books, 
            TimeSpan.FromMinutes(_readerBooksCacheTtlInMinutes));
        return result.books;
    }
}