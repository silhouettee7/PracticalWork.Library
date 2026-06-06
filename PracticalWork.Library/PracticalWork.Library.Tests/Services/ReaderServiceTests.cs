using AutoFixture;
using Domain.Abstractions.MessageBroker;
using Domain.Abstractions.Services;
using Domain.Events;
using Domain.Exceptions;
using Domain.Options;
using Microsoft.Extensions.Options;
using Moq;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Application.Services;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Tests.Services;

public class ReaderServiceTests
{
    private readonly Mock<IReaderRepository> _readerRepositoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IRabbitMqPublisher> _publisherMock;
    private readonly Fixture _fixture = new();
    private readonly ReaderService _readerService;
    
    private readonly string _rabbitLibraryExchangeName;
    private readonly string _rabbitLibraryReaderCreateKey;
    private readonly string _rabbitLibraryReaderCloseKey;
    private readonly string _redisReadersVersionKey;
    private readonly string _redisReaderBooksPrefix;
    private readonly TimeSpan _redisReaderBooksTtl;
    private readonly DateTimeOffset _fixedDateTimeProvider = new(2024, 01, 15, 10, 30, 00, TimeSpan.Zero);
    
    public ReaderServiceTests()
    {
        _readerRepositoryMock = new Mock<IReaderRepository>();
        _cacheServiceMock = new Mock<ICacheService>();
        _publisherMock = new Mock<IRabbitMqPublisher>();
        
        var timeProviderMock = new Mock<TimeProvider>();
        timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(_fixedDateTimeProvider);
        
        _rabbitLibraryExchangeName = "test-exchange";
        _rabbitLibraryReaderCreateKey = "reader.create";
        _rabbitLibraryReaderCloseKey = "reader.close";
        
        var rabbitOptions = new RabbitOptions
        {
            Library = new LibraryRabbitConfig
            {
                ExchangeName = _rabbitLibraryExchangeName,
                ReaderCreate = new QueueBindingConfig
                {
                    RoutingKey = _rabbitLibraryReaderCreateKey
                },
                ReaderClose = new QueueBindingConfig
                {
                    RoutingKey = _rabbitLibraryReaderCloseKey
                }
            }
        };
        
        var rabbitOptionsMonitor = new Mock<IOptionsMonitor<RabbitOptions>>();
        rabbitOptionsMonitor
            .Setup(x => x.CurrentValue)
            .Returns(rabbitOptions);
        
        _redisReadersVersionKey = "readers:version:key";
        _redisReaderBooksPrefix = "readers:books";
        _redisReaderBooksTtl = TimeSpan.FromMinutes(10);
        
        var redisOptions = new RedisOptions
        {
            Readers = new ReadersCacheConfig
            {
                VersionKey = _redisReadersVersionKey,
                ReaderBooks = new CacheEntryConfig
                {
                    Prefix = _redisReaderBooksPrefix,
                    TtlInMinutes = _redisReaderBooksTtl.Minutes
                }
            }
        };
        
        var redisOptionsMonitor = new Mock<IOptionsMonitor<RedisOptions>>();
        redisOptionsMonitor
            .Setup(x => x.CurrentValue)
            .Returns(redisOptions);
        
        _readerService = new ReaderService(
            _readerRepositoryMock.Object,
            _cacheServiceMock.Object,
            _publisherMock.Object,
            rabbitOptionsMonitor.Object,
            redisOptionsMonitor.Object,
            timeProviderMock.Object);
    }
    
    #region CreateReader Tests
    
    [Fact]
    public async Task CreateReader_Success_ShouldReturnReaderId()
    {
        // Arrange
        var reader = _fixture.Build<Reader>()
            .With(r => r.FullName)
            .With(r => r.PhoneNumber)
            .With(r => r.ExpiryDate, new DateOnly(2025, 12, 31))
            .Without(r => r.BorrowBooks)
            .Create();
        
        var expectedId = _fixture.Create<Guid>();
        
        _readerRepositoryMock
            .Setup(x => x.IsExistReader(reader.PhoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _readerRepositoryMock
            .Setup(x => x.CreateReader(reader, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);
        
        // Act
        var result = await _readerService.CreateReader(reader, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Equal(expectedId, result);
        Assert.True(reader.IsActive);
        
        _publisherMock.Verify(x => x.PublishAsync(
            _rabbitLibraryExchangeName,
            _rabbitLibraryReaderCreateKey,
            It.Is<ReaderCreatedEvent>(e =>
                e.ReaderId == expectedId &&
                e.FullName == reader.FullName &&
                e.PhoneNumber == reader.PhoneNumber &&
                e.ExpiryDate == reader.ExpiryDate &&
                e.CreatedAt == _fixedDateTimeProvider.DateTime),
            It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task CreateReader_PhoneNumberNotUnique_ShouldThrowReaderServiceException()
    {
        // Arrange
        var reader = _fixture.Build<Reader>()
            .With(r => r.PhoneNumber)
            .Without(r => r.ExpiryDate)
            .Without(r => r.BorrowBooks)
            .Create();
        
        _readerRepositoryMock
            .Setup(x => x.IsExistReader(reader.PhoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ReaderServiceException>(
            () => _readerService.CreateReader(reader, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Phone number is not unique", exception.Message);
        
        _readerRepositoryMock.Verify(x => x.CreateReader(
            It.IsAny<Reader>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ReaderCreatedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task CreateReader_Success_ShouldSetIsActiveToTrueAlways()
    {
        // Arrange
        var reader = _fixture.Build<Reader>()
            .With(r => r.PhoneNumber)
            .With(r => r.IsActive, false)
            .Without(r => r.ExpiryDate)
            .Without(r => r.BorrowBooks)
            .Create();
        
        _readerRepositoryMock
            .Setup(x => x.IsExistReader(reader.PhoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _readerRepositoryMock
            .Setup(x => x.CreateReader(reader, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<Guid>());
        
        // Act
        await _readerService.CreateReader(reader, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.True(reader.IsActive);
    }
    
    [Fact]
    public async Task CreateReader_RepositoryThrowsException_ShouldThrowReaderServiceException()
    {
        // Arrange
        var reader = _fixture.Build<Reader>()
            .With(r => r.PhoneNumber)
            .Without(r => r.ExpiryDate)
            .Without(r => r.BorrowBooks)
            .Create();
        
        _readerRepositoryMock
            .Setup(x => x.IsExistReader(reader.PhoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _readerRepositoryMock
            .Setup(x => x.CreateReader(reader, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _readerService.CreateReader(reader, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Database error", exception.Message);
        
        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ReaderCreatedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    #endregion
    
    #region ExtendExpiryDate Tests
    
    [Fact]
    public async Task ExtendExpiryDate_Success_ShouldExtendAndUpdate()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var oldExpiryDate = new DateOnly(2024, 12, 31);
        var newExpiryDate = new DateOnly(2025, 12, 31);
        
        var reader = _fixture.Build<Reader>()
            .With(r => r.IsActive, true)
            .With(r => r.ExpiryDate, oldExpiryDate)
            .Without(r => r.BorrowBooks)
            .Create();
            
        _readerRepositoryMock
            .Setup(x => x.GetReader(readerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reader);
        
        // Act
        await _readerService.ExtendExpiryDate(readerId, newExpiryDate, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Equal(newExpiryDate, reader.ExpiryDate);
        
        _readerRepositoryMock.Verify(x => x.UpdateReader(readerId, reader, 
            It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task ExtendExpiryDate_ReaderNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var newExpiryDate = new DateOnly(2025, 12, 31);
        
        _readerRepositoryMock
            .Setup(x => x.GetReader(readerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityNotFoundException("Читатель не найден"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _readerService.ExtendExpiryDate(readerId, newExpiryDate, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Читатель не найден", exception.Message);
        
        _readerRepositoryMock.Verify(x => x.UpdateReader(It.IsAny<Guid>(), 
            It.IsAny<Reader>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task ExtendExpiryDate_ReaderIsNotActive_ShouldThrowReaderServiceException()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var newExpiryDate = new DateOnly(2025, 12, 31);
        
        var reader = _fixture.Build<Reader>()
            .With(r => r.IsActive, false)
            .With(r => r.ExpiryDate, new DateOnly(2024, 12, 31))
            .Without(r => r.BorrowBooks)
            .Create();
        
        _readerRepositoryMock
            .Setup(x => x.GetReader(readerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reader);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ReaderServiceException>(
            () => _readerService.ExtendExpiryDate(readerId, newExpiryDate, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Карточка неактивна", exception.Message);
        
        _readerRepositoryMock.Verify(x => x.UpdateReader(It.IsAny<Guid>(), 
            It.IsAny<Reader>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Theory]
    [InlineData(2024, 12, 31, 2024, 12, 30)] 
    [InlineData(2024, 12, 31, 2024, 12, 31)]
    public async Task ExtendExpiryDate_NewDateNotInFuture_ShouldThrowReaderServiceException(
        int oldYear, int oldMonth, int oldDay,
        int newYear, int newMonth, int newDay)
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var oldExpiryDate = new DateOnly(oldYear, oldMonth, oldDay);
        var newExpiryDate = new DateOnly(newYear, newMonth, newDay);
        
        var reader = _fixture.Build<Reader>()
            .With(r => r.IsActive, true)
            .With(r => r.ExpiryDate, oldExpiryDate)
            .Without(r => r.BorrowBooks)
            .Create();
        
        _readerRepositoryMock
            .Setup(x => x.GetReader(readerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reader);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ReaderServiceException>(
            () => _readerService.ExtendExpiryDate(readerId, newExpiryDate, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Необходимо продлить карточку на будущую дату", exception.Message);
        
        _readerRepositoryMock.Verify(x => x.UpdateReader(It.IsAny<Guid>(), 
            It.IsAny<Reader>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task ExtendExpiryDate_MaxDate_ShouldWorkCorrectly()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var maxDate = new DateOnly(9999, 12, 31);
        
        var reader = _fixture.Build<Reader>()
            .With(r => r.IsActive, true)
            .With(r => r.ExpiryDate, new DateOnly(2024, 12, 31))
            .Without(r => r.BorrowBooks)
            .Create();
        
        _readerRepositoryMock
            .Setup(x => x.GetReader(readerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reader);
        
        // Act
        await _readerService.ExtendExpiryDate(readerId, maxDate, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Equal(maxDate, reader.ExpiryDate);
        
        _readerRepositoryMock.Verify(x => x.UpdateReader(readerId, reader, 
            It.IsAny<CancellationToken>()), Times.Once);
    }
    #endregion
    
    #region CloseReader Tests
    
    [Fact]
    public async Task CloseReader_Success_WhenNoBorrowBooks_ShouldDeactivateAndReturnFalse()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var reader = _fixture.Build<Reader>()
            .With(r => r.IsActive, true)
            .With(r => r.ExpiryDate, new DateOnly(2025, 12, 31))
            .With(r => r.BorrowBooks, new List<Book>())
            .Create();
        
        _readerRepositoryMock
            .Setup(x => x.GetReaderWithBorrowBooks(readerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reader);
        
        // Act
        var (borrowBooksExist, borrowBooks) = await _readerService.CloseReader(readerId, 
            It.IsAny<CancellationToken>());
        
        // Assert
        Assert.False(borrowBooksExist);
        Assert.Empty(borrowBooks);
        Assert.False(reader.IsActive);
        Assert.Equal(DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime), reader.ExpiryDate);
        
        _readerRepositoryMock.Verify(x => x.UpdateReader(readerId, reader, 
            It.IsAny<CancellationToken>()), Times.Once);
        
        _publisherMock.Verify(x => x.PublishAsync(
            _rabbitLibraryExchangeName,
            _rabbitLibraryReaderCloseKey,
            It.Is<ReaderClosedEvent>(e =>
                e.ReaderId == readerId &&
                e.FullName == reader.FullName &&
                e.ClosedAt == _fixedDateTimeProvider.DateTime &&
                e.Reason == "Вызван метод закрытия карточки"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task CloseReader_WhenHasBorrowBooks_ShouldReturnBooksWithoutDeactivating()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var borrowBooks = _fixture.Build<Book>()
            .Without(b => b.IssuanceRecords)
            .CreateMany(3)
            .ToList();
        
        var reader = _fixture.Build<Reader>()
            .With(r => r.IsActive, true)
            .With(r => r.ExpiryDate, new DateOnly(2025, 12, 31))
            .With(r => r.BorrowBooks, borrowBooks)
            .Create();
        
        _readerRepositoryMock
            .Setup(x => x.GetReaderWithBorrowBooks(readerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reader);
        
        // Act
        var (borrowBooksExist, borrowBooksResult) = await _readerService
            .CloseReader(readerId, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.True(borrowBooksExist);
        Assert.Equal(borrowBooks.Count, borrowBooksResult.Count);
        Assert.Equivalent(borrowBooks[0], borrowBooksResult[0]);
        Assert.Equivalent(borrowBooks[1], borrowBooksResult[1]);
        Assert.Equivalent(borrowBooks[2], borrowBooksResult[2]);
        Assert.True(reader.IsActive);
        Assert.NotEqual(DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime), reader.ExpiryDate);
        
        _readerRepositoryMock.Verify(x => x.UpdateReader(It.IsAny<Guid>(), 
            It.IsAny<Reader>(), It.IsAny<CancellationToken>()), Times.Never);
        
        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ReaderClosedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task CloseReader_ReaderNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        
        _readerRepositoryMock
            .Setup(x => x.GetReaderWithBorrowBooks(readerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityNotFoundException("Читатель не найден"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _readerService.CloseReader(readerId, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Читатель не найден", exception.Message);
        
        _readerRepositoryMock.Verify(x => x.UpdateReader(It.IsAny<Guid>(), 
            It.IsAny<Reader>(), It.IsAny<CancellationToken>()), Times.Never);
        
        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ReaderClosedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task CloseReader_AlreadyInactiveReader_ShouldThrowsReaderServiceException()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var reader = _fixture.Build<Reader>()
            .With(r => r.IsActive, false)
            .Without(r => r.ExpiryDate)
            .With(r => r.BorrowBooks, new List<Book>())
            .Create();
        
        _readerRepositoryMock
            .Setup(x => x.GetReaderWithBorrowBooks(readerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reader);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ReaderServiceException>(() => _readerService
            .CloseReader(readerId, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Карточка уже закрыта", exception.Message);
        Assert.False(reader.IsActive);
        
        _readerRepositoryMock.Verify(x => x.UpdateReader(It.IsAny<Guid>(), 
            It.IsAny<Reader>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<ReaderClosedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task CloseReader_ReaderWithManyBorrowBooks_ShouldReturnAllBooks()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var manyBooks = _fixture.Build<Book>()
            .Without(b => b.IssuanceRecords)
            .CreateMany(100)
            .ToList();
        
        var reader = _fixture.Build<Reader>()
            .With(r => r.IsActive, true)
            .Without(r => r.ExpiryDate)
            .With(r => r.BorrowBooks, manyBooks)
            .Create();
        
        _readerRepositoryMock
            .Setup(x => x.GetReaderWithBorrowBooks(readerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reader);
        
        // Act
        var (borrowBooksExist, borrowBooks) = await _readerService
            .CloseReader(readerId,It.IsAny<CancellationToken>());
        
        // Assert
        Assert.True(borrowBooksExist);
        Assert.Equal(100, borrowBooks.Count);
    }
    #endregion
    
    #region GetAllBorrowBooks Tests
    
    [Fact]
    public async Task GetAllBorrowBooks_CacheHit_ShouldReturnCachedResult()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var cachedBooks = _fixture
            .Build<BorrowedBook>()
            .Without(b => b.BorrowDate)
            .Without(b => b.DueDate)
            .Without(b => b.ReturnDate)
            .CreateMany(3)
            .ToList();
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisReadersVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisReaderBooksPrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<BorrowedBook>>(cacheKey))
            .ReturnsAsync(cachedBooks);
        
        // Act
        var result = await _readerService
            .GetAllBorrowBooks(readerId, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Same(cachedBooks, result);
        
        _readerRepositoryMock.Verify(
            x => x.GetReadersBorrowBooks(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), 
            Times.Never);
        
        _cacheServiceMock.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>()), 
            Times.Never);
    }
    
    [Fact]
    public async Task GetAllBorrowBooks_CacheMiss_ShouldQueryDatabaseAndCacheResult()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var expectedBooks = _fixture
            .Build<BorrowedBook>()
            .Without(b => b.BorrowDate)
            .Without(b => b.DueDate)
            .Without(b => b.ReturnDate)
            .CreateMany(3)
            .ToList();
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisReadersVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisReaderBooksPrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<BorrowedBook>>(cacheKey))
            .ReturnsAsync((IReadOnlyList<BorrowedBook>)null!);
        
        _readerRepositoryMock
            .Setup(x => x.GetReadersBorrowBooks(readerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((isActive: true, books: expectedBooks));
        
        // Act
        var result = await _readerService
            .GetAllBorrowBooks(readerId, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Equivalent(expectedBooks[0], result[0]);
        Assert.Equivalent(expectedBooks[1], result[1]);
        Assert.Equivalent(expectedBooks[2], result[2]);
        Assert.Equal(expectedBooks.Count, result.Count);
        
        _cacheServiceMock.Verify(
            x => x.SetAsync(
                cacheKey,
                expectedBooks as IReadOnlyList<BorrowedBook>,
                _redisReaderBooksTtl),
            Times.Once);
    }
    
    [Fact]
    public async Task GetAllBorrowBooks_ReaderIsNotActive_ShouldThrowReaderServiceException()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisReadersVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisReaderBooksPrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<BorrowedBook>>(cacheKey))
            .ReturnsAsync((IReadOnlyList<BorrowedBook>)null!);
        
        _readerRepositoryMock
            .Setup(x => x.GetReadersBorrowBooks(readerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((isActive: false, books: new List<BorrowedBook>()));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ReaderServiceException>(
            () => _readerService.GetAllBorrowBooks(readerId, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Карточка неактивна", exception.Message);
        
        _cacheServiceMock.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>()), 
            Times.Never);
    }
    
    [Fact]
    public async Task GetAllBorrowBooks_EmptyList_ShouldReturnEmptyAndCacheIt()
    {
        // Arrange
        var readerId = _fixture.Create<Guid>();
        var emptyBooks = new List<BorrowedBook>();
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisReadersVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisReaderBooksPrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<BorrowedBook>>(cacheKey))
            .ReturnsAsync((IReadOnlyList<BorrowedBook>)null!);
        
        _readerRepositoryMock
            .Setup(x => x.GetReadersBorrowBooks(readerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((isActive: true, books: emptyBooks));
        
        // Act
        var result = await _readerService.GetAllBorrowBooks(readerId, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Empty(result);
        
        _cacheServiceMock.Verify(
            x => x.SetAsync(
                cacheKey,
                emptyBooks as IReadOnlyList<BorrowedBook>,
                _redisReaderBooksTtl),
            Times.Once);
    }
    
    [Fact]
    public async Task GetAllBorrowBooks_MultipleReadersDifferentCacheVersions_ShouldUseCorrectKeys()
    {
        // Arrange
        var readerId1 = _fixture.Create<Guid>();
        var readerId2 = _fixture.Create<Guid>();
        var cacheVersion1 = 1L;
        var cacheVersion2 = 2L;
        var cacheKey1 = "cache-key-1";
        var cacheKey2 = "cache-key-2";
        var borrowedBooks1 = _fixture
            .Build<BorrowedBook>()
            .Without(b => b.BorrowDate)
            .Without(b => b.DueDate)
            .Without(b => b.ReturnDate)
            .CreateMany(2)
            .ToList();
        var borrowedBooks2 = _fixture
            .Build<BorrowedBook>()
            .Without(b => b.BorrowDate)
            .Without(b => b.DueDate)
            .Without(b => b.ReturnDate)
            .CreateMany(1)
            .ToList();
        
        // Первый читатель
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisReadersVersionKey))
            .ReturnsAsync(cacheVersion1);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisReaderBooksPrefix, cacheVersion1, null))
            .Returns(cacheKey1);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<BorrowedBook>>(cacheKey1))
            .ReturnsAsync((IReadOnlyList<BorrowedBook>)null!);
        
        _readerRepositoryMock
            .Setup(x => x.GetReadersBorrowBooks(readerId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((isActive: true, books: borrowedBooks1));
        
        // Второй читатель
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisReadersVersionKey))
            .ReturnsAsync(cacheVersion2);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisReaderBooksPrefix, cacheVersion2, null))
            .Returns(cacheKey2);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<BorrowedBook>>(cacheKey2))
            .ReturnsAsync((IReadOnlyList<BorrowedBook>)null!);
        
        _readerRepositoryMock
            .Setup(x => x.GetReadersBorrowBooks(readerId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((isActive: true, books: borrowedBooks2));
        
        // Act
        var result1 = await _readerService
            .GetAllBorrowBooks(readerId1,It.IsAny<CancellationToken>());
        var result2 = await _readerService
            .GetAllBorrowBooks(readerId2, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Equal(2, result1.Count);
        Assert.Equivalent(borrowedBooks1[0], result1[0]);
        Assert.Equivalent(borrowedBooks1[1], result1[1]);
        Assert.Single(result2);
        Assert.Equivalent(borrowedBooks2[0], result2[0]);
    }
    
    #endregion
}