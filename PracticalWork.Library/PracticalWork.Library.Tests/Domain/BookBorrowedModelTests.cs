using AutoFixture;
using Moq;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Tests.Domain;

public class BookBorrowTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly DateTimeOffset _fixedDateTimeOffset;
    
    public BookBorrowTests()
    {
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        _timeProviderMock = new Mock<TimeProvider>();
        _fixedDateTimeOffset = new DateTimeOffset(2024, 01, 15, 10, 30, 00, TimeSpan.Zero);
        _timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(_fixedDateTimeOffset);
    }
    
    #region CreateBookBorrow Tests
    
    [Fact]
    public void CreateBookBorrow_ShouldCreateValidBookBorrow()
    {
        // Arrange & Act
        var bookBorrow = BookBorrow.CreateBookBorrow(_timeProviderMock.Object);
        
        // Assert
        var expectedBorrowDate = DateOnly.FromDateTime(_fixedDateTimeOffset.DateTime);
        var expectedDueDate = expectedBorrowDate.AddDays(30);
        
        Assert.Equal(expectedBorrowDate, bookBorrow.BorrowDate);
        Assert.Equal(expectedDueDate, bookBorrow.DueDate);
        Assert.Equal(BookIssueStatus.Issued, bookBorrow.Status);
        Assert.Equal(default, bookBorrow.ReturnDate);
    }
    
    [Fact]
    public void CreateBookBorrow_ShouldCreateNewInstanceEachTime()
    {
        // Arrange & Act
        var bookBorrow1 = BookBorrow.CreateBookBorrow(_timeProviderMock.Object);
        var bookBorrow2 = BookBorrow.CreateBookBorrow(_timeProviderMock.Object);
        
        // Assert
        Assert.NotSame(bookBorrow1, bookBorrow2);
    }
    
    #endregion
    
    #region ReturnBookBorrow Tests
    
    [Fact]
    public void ReturnBookBorrow_WhenReturnedOnTime_ShouldSetStatusToReturned()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Borrow)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var bookBorrow = new BookBorrow
        {
            BorrowDate = new DateOnly(2024, 01, 01),
            DueDate = new DateOnly(2024, 01, 31),
            Status = BookIssueStatus.Issued,
            Book = book
        };
        
        _timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(new DateTimeOffset(2024, 01, 15, 10, 30, 00, TimeSpan.Zero));
        
        // Act
        bookBorrow.ReturnBookBorrow(_timeProviderMock.Object);
        
        // Assert
        var expectedReturnDate = DateOnly.FromDateTime(_timeProviderMock.Object.GetUtcNow().DateTime);
        Assert.Equal(expectedReturnDate, bookBorrow.ReturnDate);
        Assert.Equal(BookIssueStatus.Returned, bookBorrow.Status);
        Assert.Equal(BookStatus.Available, bookBorrow.Book.Status);
    }
    
    [Fact]
    public void ReturnBookBorrow_WhenReturnedLate_ShouldSetStatusToOverdue()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Borrow)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var bookBorrow = new BookBorrow
        {
            BorrowDate = new DateOnly(2024, 01, 01),
            DueDate = new DateOnly(2024, 01, 31),
            Status = BookIssueStatus.Issued,
            Book = book
        };
        
        _timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(new DateTimeOffset(2024, 02, 15, 10, 30, 00, TimeSpan.Zero));
        
        // Act
        bookBorrow.ReturnBookBorrow(_timeProviderMock.Object);
        
        // Assert
        var expectedReturnDate = DateOnly.FromDateTime(_timeProviderMock.Object.GetUtcNow().DateTime);
        Assert.Equal(expectedReturnDate, bookBorrow.ReturnDate);
        Assert.Equal(BookIssueStatus.Overdue, bookBorrow.Status);
        Assert.Equal(BookStatus.Available, bookBorrow.Book.Status);
    }
    
    [Fact]
    public void ReturnBookBorrow_WhenReturnedExactlyOnDueDate_ShouldSetStatusToReturned()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Borrow)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var bookBorrow = new BookBorrow
        {
            BorrowDate = new DateOnly(2024, 01, 01),
            DueDate = new DateOnly(2024, 01, 31),
            Status = BookIssueStatus.Issued,
            Book = book
        };
        
        _timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(new DateTimeOffset(2024, 01, 31, 23, 59, 59, TimeSpan.Zero));
        
        // Act
        bookBorrow.ReturnBookBorrow(_timeProviderMock.Object);
        
        // Assert
        Assert.Equal(BookIssueStatus.Returned, bookBorrow.Status);
        Assert.Equal(BookStatus.Available, bookBorrow.Book.Status);
    }
    
    [Fact]
    public void ReturnBookBorrow_ShouldUpdateBookStatusToAvailable()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Borrow)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var bookBorrow = new BookBorrow
        {
            BorrowDate = new DateOnly(2024, 01, 01),
            DueDate = new DateOnly(2024, 01, 31),
            Status = BookIssueStatus.Issued,
            Book = book
        };
        
        // Act
        bookBorrow.ReturnBookBorrow(_timeProviderMock.Object);
        
        // Assert
        Assert.Equal(BookStatus.Available, book.Status);
    }
    
    #endregion
}