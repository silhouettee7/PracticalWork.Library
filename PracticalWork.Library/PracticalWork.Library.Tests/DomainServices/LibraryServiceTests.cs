using AutoFixture;
using Domain.Abstractions.MessageBroker;
using Domain.Abstractions.Services;
using Domain.Events;
using Domain.Exceptions;
using Domain.Models;
using Domain.Options;
using Microsoft.Extensions.Options;
using Moq;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Application.Services;
using PracticalWork.Library.Dtos;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Tests.DomainServices;

public class LibraryServiceTests
{
    private readonly Mock<IReaderRepository> _readerRepositoryMock;
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IBorrowRepository> _borrowRepositoryMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<ICursorPaginationService<Book>> _paginationServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IRabbitMqPublisher> _publisherMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly Fixture _fixture = new();
    private readonly LibraryService _libraryService;
    
    private readonly string _rabbitLibraryExchangeName;
    private readonly string _rabbitLibraryBookBorrowKey;
    private readonly string _rabbitLibraryBookReturnKey;
    private readonly string _redisBooksVersionKey;
    private readonly string _redisLibraryBooksPrefix;
    private readonly TimeSpan _redisLibraryBooksTtl;
    private readonly string _redisBooksDetailsPrefix;
    private readonly TimeSpan _redisBooksDetailsTtl;
    private readonly string _redisReadersVersionKey;
    private readonly string _minioCoversBucketName;
    
    public LibraryServiceTests()
    {
        _readerRepositoryMock = new Mock<IReaderRepository>();
        _bookRepositoryMock = new Mock<IBookRepository>();
        _borrowRepositoryMock = new Mock<IBorrowRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _paginationServiceMock = new Mock<ICursorPaginationService<Book>>();
        _cacheServiceMock = new Mock<ICacheService>();
        _publisherMock = new Mock<IRabbitMqPublisher>();
        
        _timeProviderMock = new Mock<TimeProvider>();
        _timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(new DateTimeOffset(2024, 01, 15, 10, 30, 00, TimeSpan.Zero));
        
        _minioCoversBucketName = "test-covers-bucket";
        var minioOptions = new MinioOptions
        {
            CoversBucketName = _minioCoversBucketName
        };
        var minioOptionsMonitor = new Mock<IOptionsMonitor<MinioOptions>>();
        minioOptionsMonitor
            .Setup(x => x.CurrentValue)
            .Returns(minioOptions);

        _rabbitLibraryExchangeName = "test-exchange";
        _rabbitLibraryBookBorrowKey = "book.borrow";
        _rabbitLibraryBookReturnKey = "book.return";
        
        var rabbitOptions = new RabbitOptions
        {
            Library = new LibraryRabbitConfig
            {
                ExchangeName = _rabbitLibraryExchangeName,
                BookBorrow = new QueueBindingConfig
                {
                    RoutingKey = _rabbitLibraryBookBorrowKey
                },
                BookReturn = new QueueBindingConfig
                {
                    RoutingKey = _rabbitLibraryBookReturnKey
                }
            }
        };
        
        var rabbitOptionsMonitor = new Mock<IOptionsMonitor<RabbitOptions>>();
        rabbitOptionsMonitor
            .Setup(x => x.CurrentValue)
            .Returns(rabbitOptions);
        
        _redisBooksVersionKey = "books:version:key";
        _redisLibraryBooksPrefix = "library:books";
        _redisLibraryBooksTtl = TimeSpan.FromMinutes(5);
        _redisBooksDetailsPrefix = "books:details";
        _redisBooksDetailsTtl = TimeSpan.FromMinutes(10);
        _redisReadersVersionKey = "readers:version:key";
        
        var redisOptions = new RedisOptions
        {
            Books = new BooksCacheConfig
            {
                VersionKey = _redisBooksVersionKey,
                LibraryBooks = new CacheEntryConfig
                {
                    Prefix = _redisLibraryBooksPrefix,
                    TtlInMinutes = _redisLibraryBooksTtl.Minutes
                },
                BookDetails = new CacheEntryConfig
                {
                    Prefix = _redisBooksDetailsPrefix,
                    TtlInMinutes = _redisBooksDetailsTtl.Minutes
                }
            },
            Readers = new ReadersCacheConfig
            {
                VersionKey = _redisReadersVersionKey
            }
        };
        
        var redisOptionsMonitor = new Mock<IOptionsMonitor<RedisOptions>>();
        redisOptionsMonitor
            .Setup(x => x.CurrentValue)
            .Returns(redisOptions);
        
        _libraryService = new LibraryService(
            _readerRepositoryMock.Object,
            _bookRepositoryMock.Object,
            _borrowRepositoryMock.Object,
            _fileStorageServiceMock.Object,
            _paginationServiceMock.Object,
            _publisherMock.Object,
            _cacheServiceMock.Object,
            minioOptionsMonitor.Object,
            rabbitOptionsMonitor.Object,
            redisOptionsMonitor.Object,
            _timeProviderMock.Object);
    }
    
    #region BorrowBook Tests
    
    [Fact]
    public async Task BorrowBook_Success_ShouldBorrowBookAndInvalidateCaches()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var readerId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Available)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var reader = _fixture.Build<Reader>()
            .With(r => r.IsActive, true)
            .Without(r => r.ExpiryDate)
            .Without(r => r.BorrowBooks)
            .Create();
 
        var bookBorrow = BookBorrow.CreateBookBorrow(_timeProviderMock.Object);
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, cancellationToken))
            .ReturnsAsync(book);
        
        _readerRepositoryMock
            .Setup(x => x.GetReader(readerId, cancellationToken))
            .ReturnsAsync(reader);
        
        // Act
        await _libraryService.BorrowBook(bookId, readerId, cancellationToken);
        
        // Assert
        Assert.Equal(BookStatus.Borrow, book.Status);
        
        _bookRepositoryMock.Verify(x => x.UpdateBook(bookId, book, cancellationToken), Times.Once);
        _borrowRepositoryMock.Verify(x => x.CreateBookBorrow(bookId, readerId, 
            It.Is<BookBorrow>(m => 
                m.Status == bookBorrow.Status && 
                m.BorrowDate == bookBorrow.BorrowDate && 
                m.DueDate == bookBorrow.DueDate), cancellationToken), Times.Once);
        
        _publisherMock.Verify(x => x.PublishAsync(
            _rabbitLibraryExchangeName,
            _rabbitLibraryBookBorrowKey,
            It.Is<BookBorrowedEvent>(e =>
                e.BookId == bookId &&
                e.ReaderId == readerId &&
                e.ReaderName == reader.FullName &&
                e.BookTitle == book.Title &&
                e.BorrowDate == bookBorrow.BorrowDate &&
                e.DueDate == bookBorrow.DueDate),
            cancellationToken), Times.Once);
        
        _cacheServiceMock.Verify(x => x.InvalidateCache(_redisBooksVersionKey), Times.Once);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_redisReadersVersionKey), Times.Once);
    }
    
    [Fact]
    public async Task BorrowBook_BookNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var readerId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, cancellationToken))
            .ThrowsAsync(new EntityNotFoundException("Книга не найдена"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _libraryService.BorrowBook(bookId, readerId, cancellationToken));
        
        Assert.Equal("Книга не найдена", exception.Message);
        
        _bookRepositoryMock.Verify(x => x.UpdateBook(It.IsAny<Guid>(), It.IsAny<Book>(), cancellationToken), Times.Never);
        _borrowRepositoryMock.Verify(x => x.CreateBookBorrow(It.IsAny<Guid>(), 
            It.IsAny<Guid>(), It.IsAny<BookBorrow>(), cancellationToken), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<BookBorrowedEvent>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task BorrowBook_ReaderNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var readerId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Available)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, cancellationToken))
            .ReturnsAsync(book);
        
        _readerRepositoryMock
            .Setup(x => x.GetReader(readerId, cancellationToken))
            .ThrowsAsync(new EntityNotFoundException("Читатель не найден"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _libraryService.BorrowBook(bookId, readerId, cancellationToken));
        
        Assert.Equal("Читатель не найден", exception.Message);
        
        _bookRepositoryMock.Verify(x => x.UpdateBook(It.IsAny<Guid>(), It.IsAny<Book>(), cancellationToken), Times.Never);
        _borrowRepositoryMock.Verify(x => x.CreateBookBorrow(It.IsAny<Guid>(), 
            It.IsAny<Guid>(), It.IsAny<BookBorrow>(), cancellationToken), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<BookBorrowedEvent>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    
    [Theory]
    [InlineData(BookStatus.Borrow)]
    [InlineData(BookStatus.Archived)]
    public async Task BorrowBook_BookNotAvailable_ShouldThrowLibraryServiceException(BookStatus status)
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var readerId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        
        var book = _fixture.Build<Book>()
            .With(b => b.Status, status)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var reader = _fixture.Build<Reader>()
            .With(r => r.IsActive, true)
            .Without(r => r.ExpiryDate)
            .Without(r => r.BorrowBooks)
            .Create();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, cancellationToken))
            .ReturnsAsync(book);
        
        _readerRepositoryMock
            .Setup(x => x.GetReader(readerId, cancellationToken))
            .ReturnsAsync(reader);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<LibraryServiceException>(
            () => _libraryService.BorrowBook(bookId, readerId, cancellationToken));
        
        Assert.Equal("Нельзя выдать архивную или выданную книгу", exception.Message);
        
        _borrowRepositoryMock.Verify(x => x.CreateBookBorrow(It.IsAny<Guid>(), 
            It.IsAny<Guid>(), It.IsAny<BookBorrow>(), cancellationToken), Times.Never);
        _bookRepositoryMock.Verify(x => x.UpdateBook(It.IsAny<Guid>(), 
            It.IsAny<Book>(), cancellationToken), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<BookBorrowedEvent>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task BorrowBook_ReaderIsNotActive_ShouldThrowLibraryServiceException()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var readerId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Available)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var reader = _fixture.Build<Reader>()
            .With(r => r.IsActive, false)
            .Without(r => r.ExpiryDate)
            .Without(r => r.BorrowBooks)
            .Create();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, cancellationToken))
            .ReturnsAsync(book);
        
        _readerRepositoryMock
            .Setup(x => x.GetReader(readerId, cancellationToken))
            .ReturnsAsync(reader);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<LibraryServiceException>(
            () => _libraryService.BorrowBook(bookId, readerId, cancellationToken));
        
        Assert.Equal("Нельзя выдать книгу с неактивной карточкой", exception.Message);
        
        _borrowRepositoryMock.Verify(x => x.CreateBookBorrow(It.IsAny<Guid>(), 
            It.IsAny<Guid>(), It.IsAny<BookBorrow>(), cancellationToken), Times.Never);
        _bookRepositoryMock.Verify(x => x.UpdateBook(It.IsAny<Guid>(), 
            It.IsAny<Book>(), cancellationToken), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<BookBorrowedEvent>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    
    #endregion
    
    #region ReturnBook Tests
    
    [Fact]
    public async Task ReturnBook_Success_ShouldReturnBookAndInvalidateCaches()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var readerId = _fixture.Create<Guid>();
        var borrowId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Borrow)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var returnedBook = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Borrow)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var bookBorrow = _fixture.Build<BookBorrow>()
            .Without(b => b.BorrowDate)
            .Without(b => b.DueDate)
            .Without(b => b.ReturnDate)
            .With(bb => bb.Book, book)
            .With(bb => bb.Status, BookIssueStatus.Issued)
            .Create();
        
        var returnedBookBorrow = _fixture.Build<BookBorrow>()
            .Without(b => b.BorrowDate)
            .Without(b => b.DueDate)
            .Without(b => b.ReturnDate)
            .With(bb => bb.Book, returnedBook)
            .With(b => b.Status, BookIssueStatus.Issued)
            .Create();
        returnedBookBorrow.ReturnBookBorrow(_timeProviderMock.Object);
        
        var reader = _fixture.Build<Reader>()
            .Without(r => r.ExpiryDate)
            .Without(r => r.BorrowBooks)
            .Create();
        
        _borrowRepositoryMock
            .Setup(x => x.GetBookBorrow(bookId, readerId, cancellationToken))
            .ReturnsAsync((borrowId, bookBorrow));
        
        _readerRepositoryMock
            .Setup(x => x.GetReader(readerId, cancellationToken))
            .ReturnsAsync(reader);
        
        // Act
        await _libraryService.ReturnBook(bookId, readerId, cancellationToken);
        
        // Assert
        Assert.Equal(returnedBookBorrow.ReturnDate, bookBorrow.ReturnDate);
        Assert.Equal(returnedBookBorrow.Book.Status, bookBorrow.Book.Status);
        Assert.Equal(returnedBookBorrow.Status, bookBorrow.Status);
        
        _borrowRepositoryMock.Verify(x => x.ReturnBookBorrow(borrowId, bookBorrow, cancellationToken), Times.Once);
        
        _publisherMock.Verify(x => x.PublishAsync(
            _rabbitLibraryExchangeName,
            _rabbitLibraryBookReturnKey,
            It.Is<BookReturnedEvent>(e =>
                e.BookId == bookId &&
                e.ReaderId == readerId &&
                e.BookTitle == book.Title &&
                e.ReaderName == reader.FullName &&
                e.ReturnDate == bookBorrow.ReturnDate),
            cancellationToken), Times.Once);
        
        _cacheServiceMock.Verify(x => x.InvalidateCache(_redisBooksVersionKey), Times.Once);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_redisReadersVersionKey), Times.Once);
    }
    
    [Fact]
    public async Task ReturnBook_BorrowRecordNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var readerId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        
        _borrowRepositoryMock
            .Setup(x => x.GetBookBorrow(bookId, readerId, cancellationToken))
            .ThrowsAsync(new EntityNotFoundException("Запись о выдаче не найдена"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _libraryService.ReturnBook(bookId, readerId, cancellationToken));
        
        Assert.Equal("Запись о выдаче не найдена", exception.Message);
        
        _borrowRepositoryMock.Verify(x => x.ReturnBookBorrow(It.IsAny<Guid>(), 
            It.IsAny<BookBorrow>(), cancellationToken), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<BookReturnedEvent>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task ReturnBook_ReaderNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var readerId = _fixture.Create<Guid>();
        var borrowId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Borrow)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var bookBorrow = _fixture.Build<BookBorrow>()
            .Without(b => b.BorrowDate)
            .Without(b => b.DueDate)
            .Without(b => b.ReturnDate)
            .With(b => b.Status, BookIssueStatus.Issued)
            .With(bb => bb.Book, book)
            .Create();
        
        _borrowRepositoryMock
            .Setup(x => x.GetBookBorrow(bookId, readerId, cancellationToken))
            .ReturnsAsync((borrowId, bookBorrow));
        
        _readerRepositoryMock
            .Setup(x => x.GetReader(readerId, cancellationToken))
            .ThrowsAsync(new EntityNotFoundException("Читатель не найден"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _libraryService.ReturnBook(bookId, readerId, cancellationToken));
        
        Assert.Equal("Читатель не найден", exception.Message);
        
        _borrowRepositoryMock.Verify(x => x.ReturnBookBorrow(It.IsAny<Guid>(), 
            It.IsAny<BookBorrow>(), cancellationToken), Times.Once);
        _publisherMock.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<BookReturnedEvent>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    
    [Theory]
    [InlineData(BookIssueStatus.Returned)]
    [InlineData(BookIssueStatus.Overdue)]
    public async Task ReturnBook_BookBorrowNotIssued_ShouldThrowLibraryServiceException(BookIssueStatus status)
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var readerId = _fixture.Create<Guid>();
        var borrowId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        
        var book = _fixture.Build<Book>()
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var bookBorrow = _fixture.Build<BookBorrow>()
            .With(bb => bb.Book, book)
            .Without(b => b.BorrowDate)
            .Without(b => b.DueDate)
            .Without(b => b.ReturnDate)
            .With(bb => bb.Status, status)
            .Create();
        
        _borrowRepositoryMock
            .Setup(x => x.GetBookBorrow(bookId, readerId, cancellationToken))
            .ReturnsAsync((borrowId, bookBorrow));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<LibraryServiceException>(
            () => _libraryService.ReturnBook(bookId, readerId, cancellationToken));
        
        Assert.Equal("Нельзя вернуть уже возвращенную книгу", exception.Message);
        
        _readerRepositoryMock.Verify(x => x.GetReader(readerId, cancellationToken), Times.Never);
        _borrowRepositoryMock.Verify(x => x.ReturnBookBorrow(It.IsAny<Guid>(), 
            It.IsAny<BookBorrow>(), cancellationToken), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<BookReturnedEvent>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    
    [Theory]
    [InlineData(BookStatus.Archived)]
    [InlineData(BookStatus.Available)]
    public async Task ReturnBook_BookStatusNotBorrowed_ShouldThrowLibraryServiceException(BookStatus status)
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var readerId = _fixture.Create<Guid>();
        var borrowId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        
        var book = _fixture.Build<Book>()
            .With(b => b.Status, status)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var bookBorrow = _fixture.Build<BookBorrow>()
            .Without(b => b.BorrowDate)
            .Without(b => b.DueDate)
            .Without(b => b.ReturnDate)
            .With(bb => bb.Book, book)
            .With(bb => bb.Status, BookIssueStatus.Issued)
            .Create();
        
        _borrowRepositoryMock
            .Setup(x => x.GetBookBorrow(bookId, readerId, cancellationToken))
            .ReturnsAsync((borrowId, bookBorrow));
             
        // Act & Assert
        var exception = await Assert.ThrowsAsync<LibraryServiceException>(
            () => _libraryService.ReturnBook(bookId, readerId, cancellationToken));
        
        Assert.Equal("Книга не выдана читателю", exception.Message);
        
        _readerRepositoryMock.Verify(x => x.GetReader(readerId, cancellationToken), Times.Never);
        _borrowRepositoryMock.Verify(x => x.ReturnBookBorrow(It.IsAny<Guid>(), 
            It.IsAny<BookBorrow>(), cancellationToken), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<BookReturnedEvent>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    #endregion
    
    #region GetBookDetails Tests
    
    [Fact]
    public async Task GetBookDetails_ById_CacheHit_ShouldReturnCachedResult()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        var book = _fixture.Build<Book>()
            .Without(b => b.IssuanceRecords)
            .Create();
        var cachedDto = _fixture.Build<BookDetailsDto>()
            .With(d => d.Book, book)
            .With(d => d.Id, bookId)
            .Create();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisBooksVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisBooksDetailsPrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<BookDetailsDto>(cacheKey))
            .ReturnsAsync(cachedDto);
        
        // Act
        var result = await _libraryService.GetBookDetails(bookId, cancellationToken);
        
        // Assert
        Assert.Same(cachedDto, result);
        
        _bookRepositoryMock.Verify(x => x.GetBookById(bookId, cancellationToken), Times.Never);
        _fileStorageServiceMock.Verify(x => x.GetFileLinkAsync(It.IsAny<string>(), 
            It.IsAny<string>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<BookDetailsDto>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }
    
        
    [Fact]
    public async Task GetBookDetails_ByTitle_CacheHit_ShouldReturnCachedResult()
    {
        // Arrange
        var bookTitle = _fixture.Create<string>();
        var bookId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        var book = _fixture.Build<Book>()
            .Without(b => b.IssuanceRecords)
            .Create();
        var cachedDto = _fixture.Build<BookDetailsDto>()
            .With(d => d.Book, book)
            .With(d => d.Id, bookId)
            .Create();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisBooksVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisBooksDetailsPrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<BookDetailsDto>(cacheKey))
            .ReturnsAsync(cachedDto);
        
        // Act
        var result = await _libraryService.GetBookDetails(bookTitle, cancellationToken);
        
        // Assert
        Assert.Same(cachedDto, result);
        
        _bookRepositoryMock.Verify(x => x.GetBookByTitle(bookTitle, cancellationToken), Times.Never);
        _fileStorageServiceMock.Verify(x => x.GetFileLinkAsync(It.IsAny<string>(), 
            It.IsAny<string>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<BookDetailsDto>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }
    
    [Fact]
    public async Task GetBookDetails_ById_CacheMiss_ShouldQueryDatabaseAndCacheResult()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        var coverImagePath = _fixture.Create<string>();
        
        var book = _fixture.Build<Book>()
            .With(b => b.CoverImagePath, coverImagePath)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var fileLink = _fixture.Create<string>();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisBooksVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisBooksDetailsPrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<BookDetailsDto>(cacheKey))
            .ReturnsAsync((BookDetailsDto)null!);
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, cancellationToken))
            .ReturnsAsync(book);
        
        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_minioCoversBucketName, coverImagePath, cancellationToken))
            .ReturnsAsync(fileLink);
        
        // Act
        var result = await _libraryService.GetBookDetails(bookId, cancellationToken);
        
        // Assert
        Assert.Equal(bookId, result.Id);
        Assert.Equivalent(book, result.Book);
        Assert.Equal(fileLink, result.Book.CoverImagePath);
        
        _cacheServiceMock.Verify(x => x.SetAsync(
            cacheKey,
            It.Is<BookDetailsDto>(dto => dto.Id == bookId && 
                                         dto.Book.Title == book.Title && 
                                         dto.Book.CoverImagePath == book.CoverImagePath &&
                                         dto.Book.Authors.SequenceEqual(book.Authors) && 
                                         dto.Book.Year == book.Year),
            TimeSpan.FromMinutes(_redisBooksDetailsTtl.Minutes)), Times.Once);
    }

    [Fact] public async Task GetBookDetails_ByTitle_CacheMiss_ShouldQueryDatabaseAndCacheResult()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var bookTitle = _fixture.Create<string>();
        var cancellationToken = CancellationToken.None;
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        var coverImagePath = _fixture.Create<string>();
        
        var book = _fixture.Build<Book>()
            .With(b => b.CoverImagePath, coverImagePath)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var fileLink = _fixture.Create<string>();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisBooksVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisBooksDetailsPrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<BookDetailsDto>(cacheKey))
            .ReturnsAsync((BookDetailsDto)null!);
        
        _bookRepositoryMock
            .Setup(x => x.GetBookByTitle(bookTitle, cancellationToken))
            .ReturnsAsync((bookId, book));
        
        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_minioCoversBucketName, coverImagePath, cancellationToken))
            .ReturnsAsync(fileLink);
        
        // Act
        var result = await _libraryService.GetBookDetails(bookTitle, cancellationToken);
        
        // Assert
        Assert.Equal(bookId, result.Id);
        Assert.Equivalent(book, result.Book);
        Assert.Equal(fileLink, result.Book.CoverImagePath);
        
        _cacheServiceMock.Verify(x => x.SetAsync(
            cacheKey,
            It.Is<BookDetailsDto>(dto => dto.Id == bookId && 
                                         dto.Book.Title == book.Title && 
                                         dto.Book.CoverImagePath == book.CoverImagePath &&
                                         dto.Book.Authors.SequenceEqual(book.Authors) && 
                                         dto.Book.Year == book.Year),
            TimeSpan.FromMinutes(_redisBooksDetailsTtl.Minutes)), Times.Once);
    }
    
    [Fact]
    public async Task GetBookDetails_ById_BookHasNoCover_ShouldNotCallFileStorage()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        var book = _fixture.Build<Book>()
            .Without(b => b.CoverImagePath)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisBooksVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisBooksDetailsPrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<BookDetailsDto>(cacheKey))
            .ReturnsAsync((BookDetailsDto)null!);
            
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, cancellationToken))
            .ReturnsAsync(book);
        
        // Act
        var result = await _libraryService.GetBookDetails(bookId, cancellationToken);
        
        // Assert
        Assert.Equal(bookId, result.Id);
        Assert.Equivalent(book, result.Book);
        Assert.Null(result.Book.CoverImagePath);
        
        _fileStorageServiceMock.Verify(x => x.GetFileLinkAsync(It.IsAny<string>(), 
            It.IsAny<string>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<BookDetailsDto>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact] public async Task GetBookDetails_ByTitle_BookHasNoCover_ShouldNotCallFileStorage()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var bookTitle = _fixture.Create<string>();
        var cancellationToken = CancellationToken.None;
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        var book = _fixture.Build<Book>()
            .Without(b => b.CoverImagePath)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisBooksVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisBooksDetailsPrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<BookDetailsDto>(cacheKey))
            .ReturnsAsync((BookDetailsDto)null!);
            
        _bookRepositoryMock
            .Setup(x => x.GetBookByTitle(bookTitle, cancellationToken))
            .ReturnsAsync((bookId, book));
        
        // Act
        var result = await _libraryService.GetBookDetails(bookTitle, cancellationToken);
        
        // Assert
        Assert.Equal(bookId, result.Id);
        Assert.Equivalent(book, result.Book);
        Assert.Null(result.Book.CoverImagePath);
        
        _fileStorageServiceMock.Verify(x => x.GetFileLinkAsync(It.IsAny<string>(), 
            It.IsAny<string>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<BookDetailsDto>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }
    
    [Fact]
    public async Task GetBookDetails_ByTitle_BookNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var title = _fixture.Create<string>();
        var cancellationToken = CancellationToken.None;
        
        _bookRepositoryMock
            .Setup(x => x.GetBookByTitle(title, cancellationToken))
            .ThrowsAsync(new EntityNotFoundException("Книга не найдена"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _libraryService.GetBookDetails(title, cancellationToken));
        
        Assert.Equal("Книга не найдена", exception.Message);
        _fileStorageServiceMock.Verify(x => x.GetFileLinkAsync(It.IsAny<string>(), 
            It.IsAny<string>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<BookDetailsDto>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }
    
    [Fact]
    public async Task GetBookDetails_ById_BookNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var id = _fixture.Create<Guid>();
        var cancellationToken = CancellationToken.None;
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(id, cancellationToken))
            .ThrowsAsync(new EntityNotFoundException("Книга не найдена"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _libraryService.GetBookDetails(id, cancellationToken));
        
        Assert.Equal("Книга не найдена", exception.Message);
        _fileStorageServiceMock.Verify(x => x.GetFileLinkAsync(It.IsAny<string>(), 
            It.IsAny<string>(), cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<BookDetailsDto>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }
    
    #endregion
    
    #region GetNonArchivedBooksPage Tests
    
    [Fact]
    public async Task GetNonArchivedBooksPage_CacheHit_ShouldReturnCachedResult()
    {
        // Arrange
        var request = _fixture.Create<CursorPaginationRequest>();
        var cancellationToken = CancellationToken.None;
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        var book = _fixture.Build<Book>()
            .Without(b => b.IssuanceRecords)
            .Create();
        var cachedResponse = _fixture
            .Build<CursorPaginationResponse<Book>>()
            .With(x => x.Items, new List<Book> { book })
            .Create();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisBooksVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisLibraryBooksPrefix, cacheVersion, request))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<CursorPaginationResponse<Book>>(cacheKey))
            .ReturnsAsync(cachedResponse);
        
        // Act
        var result = await _libraryService.GetNonArchivedBooksPage(request, cancellationToken);
        
        // Assert
        Assert.Same(cachedResponse, result);
        Assert.Single(result.Items);
        Assert.Equivalent(book, result.Items[0]);
        
        _bookRepositoryMock.Verify(x => x.GetNonArchivedBooksPageWithIssuanceRecords(
            request, cancellationToken), Times.Never);
        _cacheServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), 
            It.IsAny<object>(), It.IsAny<TimeSpan?>()), Times.Never);
    }
    
    [Fact]
    public async Task GetNonArchivedBooksPage_CacheMiss_ShouldQueryDatabaseAndCacheResult()
    {
        // Arrange
        var request = _fixture.Create<CursorPaginationRequest>();
        var cancellationToken = CancellationToken.None;
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        var books = _fixture.Build<Book>()
            .Without(b => b.IssuanceRecords)
            .CreateMany(3)
            .ToList();
        var expectedResponse = _fixture
            .Build<CursorPaginationResponse<Book>>()
            .With(x => x.Items, books)
            .Create();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisBooksVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisLibraryBooksPrefix, cacheVersion, request))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<CursorPaginationResponse<Book>>(cacheKey))
            .ReturnsAsync((CursorPaginationResponse<Book>)null!);
        
        _bookRepositoryMock
            .Setup(x => x.GetNonArchivedBooksPageWithIssuanceRecords(request, cancellationToken))
            .ReturnsAsync(books);
        
        _paginationServiceMock
            .Setup(x => x.ToCursorPageResponse(books, request))
            .Returns(expectedResponse);
        
        // Act
        var result = await _libraryService.GetNonArchivedBooksPage(request, cancellationToken);
        
        // Assert
        Assert.Same(expectedResponse, result);
        Assert.Equal(3, result.Items.Count);
        Assert.Equivalent(books[0], result.Items[0]);
        Assert.Equivalent(books[1], result.Items[1]);
        Assert.Equivalent(books[2], result.Items[2]);
        
        _cacheServiceMock.Verify(x => x.SetAsync(
            cacheKey,
            expectedResponse,
            TimeSpan.FromMinutes(_redisLibraryBooksTtl.Minutes)), Times.Once);
    }
    
    [Fact]
    public async Task GetNonArchivedBooksPage_EmptyList_ShouldCacheEmptyResponse()
    {
        // Arrange
        var request = _fixture.Create<CursorPaginationRequest>();
        var cancellationToken = CancellationToken.None;
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        var emptyBooks = new List<Book>();
        var expectedResponse = new CursorPaginationResponse<Book>
        {
            Items = emptyBooks
        };
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisBooksVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisLibraryBooksPrefix, cacheVersion, request))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<CursorPaginationResponse<Book>>(cacheKey))
            .ReturnsAsync((CursorPaginationResponse<Book>)null!);
        
        _bookRepositoryMock
            .Setup(x => x.GetNonArchivedBooksPageWithIssuanceRecords(request, cancellationToken))
            .ReturnsAsync(emptyBooks);
        
        _paginationServiceMock
            .Setup(x => x.ToCursorPageResponse(emptyBooks, request))
            .Returns(expectedResponse);
        
        // Act
        var result = await _libraryService.GetNonArchivedBooksPage(request, cancellationToken);
        
        // Assert
        Assert.Empty(result.Items);
        
        _cacheServiceMock.Verify(x => x.SetAsync(cacheKey, expectedResponse, 
            TimeSpan.FromMinutes(_redisLibraryBooksTtl.Minutes)), Times.Once);
    }
    #endregion
}