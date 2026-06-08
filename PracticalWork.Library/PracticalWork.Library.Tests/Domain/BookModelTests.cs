using AutoFixture;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Tests.Domain;

public class BookTests
{
    private readonly Fixture _fixture = new();
    
    public BookTests()
    {
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
    
    #region CanBeArchived Tests
    
    [Theory]
    [InlineData(BookStatus.Available, true)]
    [InlineData(BookStatus.Archived, true)]
    [InlineData(BookStatus.Borrow, false)]
    public void CanBeArchived_ShouldReturnExpectedResult(BookStatus status, bool expected)
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Status, status)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        // Act
        var result = book.CanBeArchived();
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    #endregion
    
    #region Archive Tests
    
    [Fact]
    public void Archive_WhenBookCanBeArchived_ShouldSetStatusToArchived()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Available)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        // Act
        book.Archive();
        
        // Assert
        Assert.Equal(BookStatus.Archived, book.Status);
    }
    
    [Fact]
    public void Archive_WhenBookIsBorrowed_ShouldThrowBookServiceException()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Borrow)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        // Act & Assert
        var exception = Assert.Throws<BookServiceException>(() => book.Archive());
        
        Assert.Equal("Книга не может быть заархивирована. Она выдана читателю", exception.Message);
        Assert.Equal(BookStatus.Borrow, book.Status);
    }
    
    [Fact]
    public void Archive_WhenBookIsAlreadyArchived_ShouldThrowBookServiceException()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Status, BookStatus.Archived)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        // Act & Assert
        var exception = Assert.Throws<BookServiceException>(() => book.Archive());
        
        Assert.Equal("Попытка повторной архивации книги", exception.Message);
        Assert.Equal(BookStatus.Archived, book.Status);
    }
    
    #endregion
    
    #region Update Tests
    
    [Fact]
    public void Update_ShouldUpdateAllUpdatableProperties()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Title, "Old Title")
            .With(b => b.Description, "Old Description")
            .With(b => b.Year, 2000)
            .With(b => b.Authors, new List<string> { "Old Author" })
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var newTitle = "New Title";
        var newDescription = "New Description";
        var newYear = 2024;
        var newAuthors = new List<string> { "New Author 1", "New Author 2" };
        
        // Act
        book.Update(newTitle, newDescription, newYear, newAuthors);
        
        // Assert
        Assert.Equal(newTitle, book.Title);
        Assert.Equal(newDescription, book.Description);
        Assert.Equal(newYear, book.Year);
        Assert.Equal(newAuthors, book.Authors);
    }
    
    [Fact]
    public void Update_WithEmptyAuthors_ShouldUpdateWithEmptyList()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Authors, new List<string> { "Old Author" })
            .Without(b => b.IssuanceRecords)
            .Create();
        
        var emptyAuthors = new List<string>();
        
        // Act
        book.Update("Title", "Description", 2024, emptyAuthors);
        
        // Assert
        Assert.Empty(book.Authors);
    }
    
    [Fact]
    public void Update_WithNullDescription_ShouldSetDescriptionToNull()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Description, "Old Description")
            .Without(b => b.IssuanceRecords)
            .Create();
        
        // Act
        book.Update("Title", null!, 2024, new List<string> { "Author" });
        
        // Assert
        Assert.Null(book.Description);
    }
    
    #endregion
    
    #region IsArchived Property Tests
    
    [Theory]
    [InlineData(BookStatus.Available, false)]
    [InlineData(BookStatus.Borrow, false)]
    [InlineData(BookStatus.Archived, true)]
    public void IsArchived_ShouldReflectStatus(BookStatus status, bool expected)
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Status, status)
            .Without(b => b.IssuanceRecords)
            .Create();
        
        // Act & Assert
        Assert.Equal(expected, book.IsArchived);
    }
    
    #endregion
}