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
using PracticalWork.Library.Enums;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Tests.Services;

public class BookServiceTests
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<ICursorPaginationService<Book>> _paginationServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IRabbitMqPublisher> _publisherMock;
    private readonly Fixture _fixture = new ();
    private readonly BookService _bookService;
    
    private readonly string _redisBooksVersionKey;
    private readonly string _minioCoversBucketName;
    private readonly string _rabbitLibraryExchangeName;
    private readonly string _rabbitLibraryBookCreateKey;
    private readonly string _rabbitLibraryBookArchiveKey;
    private readonly string _redisBooksListPrefix;
    private readonly string _redisBooksDetailsPrefix;
    private readonly TimeSpan _redisBooksListTtl;
    private readonly TimeSpan _redisBooksDetailsTtl;
    
    private readonly DateTimeOffset _fixedDateTimeProvider = new 
        (2020, 01, 01, 00, 00, 00, TimeSpan.Zero);
    
    public BookServiceTests()
    {
        _bookRepositoryMock = new Mock<IBookRepository>();
        _paginationServiceMock = new Mock<ICursorPaginationService<Book>>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _publisherMock = new Mock<IRabbitMqPublisher>();
        
        Mock<TimeProvider> timeProviderMock = new();
        timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(_fixedDateTimeProvider);

        _minioCoversBucketName = "test-bucket";
        var minioOptions = new MinioOptions
        {
            CoversBucketName = _minioCoversBucketName
        };
        var minioOptionsMonitor = new Mock<IOptionsMonitor<MinioOptions>>();
        minioOptionsMonitor.Setup(x => x.CurrentValue)
            .Returns(minioOptions);
        
        _rabbitLibraryExchangeName = "test-exchange";
        _rabbitLibraryBookCreateKey = "book.create";
        _rabbitLibraryBookArchiveKey = "book.archive";
        var rabbitOptions = new RabbitOptions
        {
            Library = new LibraryRabbitConfig
            {
                ExchangeName = _rabbitLibraryExchangeName,
                BookCreate = new QueueBindingConfig { RoutingKey = _rabbitLibraryBookCreateKey },
                BookArchive = new QueueBindingConfig { RoutingKey = _rabbitLibraryBookArchiveKey },
            }
        };
        
        var rabbitOptionsMonitor = new Mock<IOptionsMonitor<RabbitOptions>>();
        rabbitOptionsMonitor.Setup(x => x.CurrentValue)
            .Returns(rabbitOptions);

        _redisBooksVersionKey = "books:version:key";
        _redisBooksListPrefix = "books:list";
        _redisBooksDetailsPrefix = "books:details";
        _redisBooksListTtl = TimeSpan.FromMinutes(1);
        _redisBooksDetailsTtl = TimeSpan.FromMinutes(1);
        var redisOptions = new RedisOptions
        {
            Books = new BooksCacheConfig
            {
                VersionKey = _redisBooksVersionKey,
                BooksList = new CacheEntryConfig
                {
                    Prefix = _redisBooksListPrefix, 
                    TtlInMinutes = _redisBooksListTtl.Minutes
                },
                BookDetails = new CacheEntryConfig
                {
                    Prefix = _redisBooksDetailsPrefix, 
                    TtlInMinutes = _redisBooksDetailsTtl.Minutes
                },
            }
        };
        
        var redisOptionsMonitor = new Mock<IOptionsMonitor<RedisOptions>>();
        redisOptionsMonitor.Setup(x => x.CurrentValue)
            .Returns(redisOptions);

        _bookService = new BookService(
            _bookRepositoryMock.Object,
            _paginationServiceMock.Object,
            _fileStorageServiceMock.Object,
            _cacheServiceMock.Object,
            _publisherMock.Object,
            minioOptionsMonitor.Object,
            rabbitOptionsMonitor.Object,
            redisOptionsMonitor.Object,
            timeProviderMock.Object);
    }
    
    #region CreateBook Tests
    
    [Fact]
    public async Task CreateBook_Success_ShouldReturnBookId()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Year)
            .With(b => b.Title)
            .With(b => b.Description)
            .With(b => b.Category)
            .With(b => b.Authors, new List<string> {_fixture.Create<string>()})
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var expectedId = _fixture.Create<Guid>();
        
        _bookRepositoryMock
            .Setup(x => x.CreateBook(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);
        
        // Act
        var result = await _bookService.CreateBook(book, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Equal(expectedId, result);
        Assert.Equal(BookStatus.Available, book.Status);

        _publisherMock.Verify(x => x.PublishAsync(
            _rabbitLibraryExchangeName,
            _rabbitLibraryBookCreateKey,
            It.Is<BookCreatedEvent>(e => 
                e.BookId == expectedId &&
                e.Authors[0] == book.Authors[0] &&
                e.Year == book.Year && 
                e.Title == book.Title && 
                e.Category == book.Category.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_redisBooksVersionKey), Times.Once);
    }
    
    [Fact]
    public async Task CreateBook_Success_ShouldSetStatusToAvailableAlways()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Year)
            .With(b => b.Title)
            .With(b => b.Description)
            .With(b => b.Category)
            .With(b => b.Authors)
            .With(b => b.Status, BookStatus.Borrow)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        // Act
        await _bookService.CreateBook(book, It.IsAny<CancellationToken>());
        
        //Assert
        Assert.Equal(BookStatus.Available, book.Status);
    }
    
    [Fact]
    public async Task CreateBook_RepositoryThrowsException_ShouldThrowBookServiceException()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Year)
            .With(b => b.Title)
            .With(b => b.Authors)
            .With(b => b.Description)
            .With(b => b.Category)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        _bookRepositoryMock
            .Setup(x => x.CreateBook(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<BookServiceException>(
            () => _bookService.CreateBook(book, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Ошибка создание книги!", exception.Message);
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<BookCreatedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    #endregion
    
    #region UpdateBook Tests
    
    [Fact]
    public async Task UpdateBook_Success_ShouldUpdateAndInvalidateCache()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        
        var existingAuthor = _fixture.Create<string>();
        var existingBook = _fixture.Build<Book>()
            .With(x => x.Title)
            .With(x => x.Authors, new List<string> {existingAuthor})
            .With(x => x.Description)
            .With(x => x.Year)
            .With(x => x.IsArchived, false)
            .With(x => x.Status, BookStatus.Available)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var newAuthor = _fixture.Create<string>();
        var updatedBook = new Book
        {
            Title = existingBook.Title + "-updated",
            Description = existingBook.Description + "-updated",
            Year = existingBook.Year + 1,
            Authors = [..existingBook.Authors, newAuthor],
        };
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBook);
        
        // Act
        await _bookService.UpdateBook(bookId, updatedBook, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Equal(updatedBook.Title, existingBook.Title);
        Assert.Equal(updatedBook.Description, existingBook.Description);
        Assert.Equal(updatedBook.Year, existingBook.Year);
        Assert.Equal(2, existingBook.Authors.Count);
        Assert.Contains(existingAuthor, existingBook.Authors);
        Assert.Contains(newAuthor, existingBook.Authors);
        
        _cacheServiceMock.Verify(x => x
            .InvalidateCache(_redisBooksVersionKey), Times.Once);
    }
    
    [Fact]
    public async Task UpdateBook_BookNotFound_ShouldThrowException()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var updatedBook = _fixture.Build<Book>()
            .Without(b => b.IssuanceRecords)
            .Create();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, 
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityNotFoundException("Book not found"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _bookService.UpdateBook(bookId, updatedBook, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Book not found", exception.Message);
        _bookRepositoryMock.Verify(x => x.UpdateBook(It.IsAny<Guid>(), 
            It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_redisBooksVersionKey), Times.Never);
    }
    
    [Theory]
    [InlineData(false, BookStatus.Archived)]
    [InlineData(true, BookStatus.Available)]
    [InlineData(true, BookStatus.Archived)]
    public async Task UpdateBook_BookIsArchived_ShouldThrowBookServiceException(bool isArchived, BookStatus bookStatus)
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var archivedBook = _fixture.Build<Book>()
            .With(b => b.IsArchived, isArchived)
            .With(b => b.Status, bookStatus)
            .Without(b => b.IssuanceRecords)
            .Create();
        var updatedBook = _fixture.Build<Book>()
            .Without(b => b.IssuanceRecords)
            .Create();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(archivedBook);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<BookServiceException>(
            () => _bookService.UpdateBook(bookId, updatedBook, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Книга в архиве", exception.Message);
        _bookRepositoryMock.Verify(x => x.UpdateBook(It.IsAny<Guid>(), 
            It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_redisBooksVersionKey), Times.Never);
    }
    
    #endregion
    
    #region ArchiveBook Tests
    
    [Fact]
    public async Task ArchiveBook_Success_ShouldArchiveAndReturnResponse()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var book = _fixture.Build<Book>()
            .With(x => x.Title)
            .With(x => x.IsArchived, false)
            .With(x => x.Status, BookStatus.Available)
            .Without(x => x.IssuanceRecords)
            .Create();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        
        // Act
        var result = await _bookService.ArchiveBook(bookId, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.True(book.IsArchived);
        Assert.Equal(BookStatus.Archived, book.Status);
        Assert.Equal(bookId, result.Id);
        Assert.Equal(book.Title, result.Title);
        Assert.Equal(_fixedDateTimeProvider.DateTime, result.ArchivedAt);
        
        _bookRepositoryMock.Verify(x => x.UpdateBook(bookId, book,
            It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.PublishAsync(
            _rabbitLibraryExchangeName,
            _rabbitLibraryBookArchiveKey,
            It.Is<BookArchivedEvent>(e =>
                e.BookId == bookId && 
                e.Title == book.Title &&
                e.ArchivedAt == _fixedDateTimeProvider.DateTime),
            It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => 
            x.InvalidateCache(_redisBooksVersionKey), Times.Once);
    }
    
    [Theory]
    [InlineData(false, BookStatus.Archived)]
    [InlineData(true, BookStatus.Available)]
    [InlineData(true, BookStatus.Archived)]
    public async Task ArchiveBook_FailedWhenBookIsArchived_ShouldThrowsBookServiceException(bool isArchived, BookStatus bookStatus)
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var book = _fixture.Build<Book>()
            .With(x => x.IsArchived, isArchived)
            .With(x => x.Status, bookStatus)
            .Without(x => x.IssuanceRecords)
            .Create();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<BookServiceException>(() =>
            _bookService.ArchiveBook(bookId, It.IsAny<CancellationToken>()));
        
        Assert.Equal("Попытка повторной архивации книги", exception.Message);
        
        _bookRepositoryMock.Verify(x => x.UpdateBook(It.IsAny<Guid>(), It.IsAny<Book>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<BookArchivedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(x => 
            x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task ArchiveBook_FailedWhenBookIsBorrowed_ShouldThrowsBookServiceException()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var book = _fixture.Build<Book>()
            .With(x => x.IsArchived, false)
            .With(x => x.Status, BookStatus.Borrow)
            .Without(x => x.IssuanceRecords)
            .Create();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<BookServiceException>(() =>
            _bookService.ArchiveBook(bookId, It.IsAny<CancellationToken>()));
        
        Assert.False(book.IsArchived);
        Assert.NotEqual(BookStatus.Archived, book.Status);
        Assert.Equal("Книга не может быть заархивирована. Она выдана читателю", exception.Message);
        
        _bookRepositoryMock.Verify(x => x.UpdateBook(It.IsAny<Guid>(), It.IsAny<Book>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<BookArchivedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(x => 
            x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task ArchiveBook_BookNotFound_ShouldThrowException()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityNotFoundException("Книга не найдена"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _bookService.ArchiveBook(bookId, It.IsAny<CancellationToken>()));
        Assert.Equal("Книга не найдена", exception.Message);
        
        _bookRepositoryMock.Verify(x => x.UpdateBook(It.IsAny<Guid>(), It.IsAny<Book>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<BookArchivedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(x => 
            x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    
    #endregion
    
    #region GetBooksPage Tests - Кэширование
    
    [Fact]
    public async Task GetBooksPage_CacheHit_ShouldReturnCachedResult()
    {
        // Arrange
        var cachedResponse = _fixture.Build<CursorPaginationResponse<Book>>()
            .Without(x => x.Items)
            .Create();
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisBooksVersionKey))
            .ReturnsAsync(cacheVersion);
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisBooksListPrefix, cacheVersion, It.IsAny<object>()))
            .Returns(cacheKey);
        _cacheServiceMock
            .Setup(x => x.GetAsync<CursorPaginationResponse<Book>>(cacheKey))
            .ReturnsAsync(cachedResponse);
        
        // Act
        var result = await _bookService.GetBooksPage(
            It.IsAny<CursorPaginationRequest>(), 
            It.IsAny<BookStatus?>(),
            It.IsAny<BookCategory>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Same(cachedResponse, result);
        
        _bookRepositoryMock.Verify(
            x => x.GetBooksPageFilteringByFields(
                It.IsAny<CursorPaginationRequest>(), 
                It.IsAny<BookStatus?>(), 
                It.IsAny<BookCategory?>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<object>(), 
            It.IsAny<TimeSpan?>()), Times.Never);
    }
    
    [Fact]
    public async Task GetBooksPage_CacheMiss_ShouldQueryDatabaseAndCacheResult()
    {
        // Arrange
        var request = _fixture.Create<CursorPaginationRequest>();
        var status = _fixture.Create<BookStatus>();
        var category = _fixture.Create<BookCategory>();
        var author = _fixture.Create<string>();
        
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        
        var dbBooks = new List<Book> 
        { 
            _fixture.Build<Book>()
                .With(x => x.Status, status)
                .With(x => x.Category, category)
                .With(x => x.Authors, [author])
                .Without(x => x.IssuanceRecords)
                .Create()
        };
        
        var expectedResponse = new CursorPaginationResponse<Book>
        {
            Items = dbBooks
        };
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_redisBooksVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_redisBooksListPrefix, cacheVersion, It.IsAny<object>()))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<CursorPaginationResponse<Book>>(cacheKey))
            .ReturnsAsync((CursorPaginationResponse<Book>)null!);

        _bookRepositoryMock
            .Setup(x => x.GetBooksPageFilteringByFields(
                request, status, category, author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbBooks);
        
        _paginationServiceMock
            .Setup(x => x.ToCursorPageResponse(dbBooks, request))
            .Returns(expectedResponse);
        
        // Act
        var result = await _bookService.GetBooksPage(
            request, status, category, author, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Same(expectedResponse, result);
        
        _cacheServiceMock.Verify(
            x => x.SetAsync(
                cacheKey,
                expectedResponse,
                _redisBooksListTtl),
            Times.Once);
    }
    
    #endregion
    
    #region AddBookDetails Tests
    
    [Fact]
    public async Task AddBookDetails_Success_ShouldUploadAndUpdateBook()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var description = _fixture.Create<string>();
        var coverStream = new MemoryStream();
        var contentType = _fixture.Create<string>();
        var currentDate = _fixedDateTimeProvider.DateTime;

        var book = _fixture.Build<Book>()
            .Without(x => x.CoverImagePath)
            .Without(x => x.Description)
            .Without(x => x.IssuanceRecords)
            .Create();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        
        // Act
        await _bookService.AddBookDetails(bookId, description, 
            coverStream, contentType, It.IsAny<CancellationToken>());
        
        // Assert
        Assert.Equal(description, book.Description);
        Assert.Contains(currentDate.Year.ToString(), book.CoverImagePath);
        Assert.Contains(currentDate.Month.ToString(), book.CoverImagePath);
        Assert.Contains(bookId.ToString(), book.CoverImagePath);
        
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(
            _minioCoversBucketName,
            It.Is<string>(m => 
                m.Contains(currentDate.Year.ToString()) && 
                m.Contains(currentDate.Month.ToString()) && 
                m.Contains(bookId.ToString())),
            coverStream,
            contentType,
            It.IsAny<CancellationToken>()), Times.Once);
        
        _bookRepositoryMock.Verify(x => x.UpdateBook(bookId, book, 
            It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_redisBooksVersionKey), Times.Once);
    }
    
    [Fact]
    public async Task AddBookDetails_BookNotFound_ShouldThrowException()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityNotFoundException("Книга не найдена"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _bookService.AddBookDetails(bookId, It.IsAny<string>(), It.IsAny<Stream>(), 
                It.IsAny<string>(), It.IsAny<CancellationToken>()));
        Assert.Equal("Книга не найдена", exception.Message);
        
        _bookRepositoryMock.Verify(x => x.UpdateBook(bookId, It.IsAny<Book>(), 
            It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_redisBooksVersionKey), Times.Never);
    }
    
    [Fact]
    public async Task AddBookDetails_UploadFails_ShouldNotUpdateBook()
    {
        // Arrange
        var bookId = _fixture.Create<Guid>();
        var book = _fixture.Build<Book>()
            .Without(x => x.CoverImagePath)
            .Without(x => x.Description)
            .Without(x => x.IssuanceRecords)
            .Create();
        
        _bookRepositoryMock
            .Setup(x => x.GetBookById(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        
        _fileStorageServiceMock
            .Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Upload failed"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _bookService.AddBookDetails(bookId, It.IsAny<string>(), It.IsAny<Stream>(), 
                It.IsAny<string>(), It.IsAny<CancellationToken>()));
        
        Assert.Equal("Upload failed", exception.Message);
        Assert.Null(book.CoverImagePath);
        Assert.Null(book.Description);
        
        _bookRepositoryMock.Verify(x => x.UpdateBook(bookId, It.IsAny<Book>(), 
            It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_redisBooksVersionKey), Times.Never);
    }
    
    #endregion
}