using Domain.Models;
using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Controllers.Mappers.v1;
using PracticalWork.Library.Models;
using BookStatus = PracticalWork.Library.Contracts.v1.Enums.BookStatus;

namespace PracticalWork.Library.Tests.Mappers;

public class BooksExtensionsTests
{
    
    [Fact]
    public void ToBook_FromCreateBookRequest_ShouldMapCorrectly()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Война и мир",
            Category: Contracts.v1.Enums.BookCategory.FictionBook,
            Authors: new List<string> { "Лев Толстой" },
            Description: "Великий роман",
            Year: 1869);
        
        // Act
        var result = request.ToBook();
        
        // Assert
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Authors, result.Authors);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.Year, result.Year);
        Assert.Equal((Enums.BookCategory)request.Category, result.Category);
    }
    
    [Fact]
    public void ToBook_FromUpdateBookRequest_ShouldMapCorrectly()
    {
        // Arrange
        var request = new UpdateBookRequest(
            Title: "Война и мир. Том 2",
            Authors: new List<string> { "Лев Толстой" },
            Description: "Продолжение великого романа",
            Year: 1870);
        
        // Act
        var result = request.ToBook();
        
        // Assert
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Authors, result.Authors);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.Year, result.Year);
    }
    
    [Fact]
    public void ToArchiveBookResponse_ShouldMapCorrectly()
    {
        // Arrange
        var bookArchive = new BookArchive
        {
            Id = Guid.NewGuid(),
            Title = "Архивная книга",
            ArchivedAt = DateTime.UtcNow
        };
        
        // Act
        var result = bookArchive.ToArchiveBookResponse();
        
        // Assert
        Assert.Equal(bookArchive.Id, result.Id);
        Assert.Equal(bookArchive.Title, result.Title);
        Assert.Equal(bookArchive.ArchivedAt, result.ArchivedAt);
    }
    
    [Fact]
    public void ToBookResponse_ShouldMapCorrectly()
    {
        // Arrange
        var book = new Book
        {
            Title = "Война и мир",
            Category = Enums.BookCategory.FictionBook,
            Authors = new List<string> { "Лев Толстой" },
            Description = "Великий роман",
            Year = 1869,
            Status = Enums.BookStatus.Available,
        };
        
        // Act
        var result = book.ToBookResponse();
        
        // Assert
        Assert.Equal(book.Title, result.Title);
        Assert.Equal((Contracts.v1.Enums.BookCategory)book.Category, result.Category);
        Assert.Equal(book.Authors, result.Authors);
        Assert.Equal(book.Description, result.Description);
        Assert.Equal(book.Year, result.Year);
        Assert.Equal((BookStatus)book.Status, result.Status);
        Assert.Equal(book.IsArchived, result.IsArchived);
    }
    
    [Fact]
    public void ToBookDetailsResponse_ShouldMapCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var book = new Book
        {
            Title = "Война и мир",
            Category = Enums.BookCategory.FictionBook,
            Authors = new List<string> { "Лев Толстой" },
            Description = "Великий роман",
            Year = 1869,
            CoverImagePath = "covers/2024/01/book.jpg",
            Status = Enums.BookStatus.Available,
        };
        
        // Act
        var result = book.ToBookDetailsResponse(id);
        
        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal(book.Title, result.Title);
        Assert.Equal((Contracts.v1.Enums.BookCategory)book.Category, result.Category);
        Assert.Equal(book.Authors, result.Authors);
        Assert.Equal(book.Description, result.Description);
        Assert.Equal(book.Year, result.Year);
        Assert.Equal(book.CoverImagePath, result.CoverImageUrl);
        Assert.Equal((BookStatus)book.Status, result.Status);
        Assert.Equal(book.IsArchived, result.IsArchived);
    }
    
    [Fact]
    public void ToBookCursorPaginationResponse_ShouldMapCorrectly()
    {
        // Arrange
        var books = new List<Book>
        {
            new() { Title = "Книга 1", Category = Enums.BookCategory.FictionBook, Authors = new List<string> { "Автор 1" }, Year = 2000 },
            new() { Title = "Книга 2", Category = Enums.BookCategory.ScientificBook, Authors = new List<string> { "Автор 2" }, Year = 2001 }
        };
        
        var response = new CursorPaginationResponse<Book>
        {
            Items = books,
            NextCursor = "cursor123",
            PreviousCursor = "cursor456",
            HasNext = true,
            HasPrevious = false
        };
        
        // Act
        var result = response.ToBookCursorPaginationResponse();
        
        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(response.NextCursor, result.NextCursor);
        Assert.Equal(response.PreviousCursor, result.PreviousCursor);
        Assert.Equal(response.HasNext, result.HasNext);
        Assert.Equal(response.HasPrevious, result.HasPrevious);
        Assert.Equal(books[0].Title, result.Items[0].Title);
        Assert.Equal(books[1].Title, result.Items[1].Title);
    }
    
    [Fact]
    public void ToBookWithIssuanceCursorPaginationResponse_ShouldMapCorrectly()
    {
        // Arrange
        var issuanceRecords = new List<BookBorrow>
        {
            new() { BorrowDate = new DateOnly(2024, 1, 1), DueDate = new DateOnly(2024, 1, 31), Status = Enums.BookIssueStatus.Issued }
        };
        
        var books = new List<Book>
        {
            new() 
            { 
                Title = "Книга 1", 
                Category = Enums.BookCategory.FictionBook, 
                Authors = new List<string> { "Автор 1" }, 
                Year = 2000,
                IssuanceRecords = issuanceRecords
            }
        };
        
        var response = new CursorPaginationResponse<Book>
        {
            Items = books,
            NextCursor = "cursor123",
            HasNext = true,
            HasPrevious = false
        };
        
        // Act
        var result = response.ToBookWithIssuanceCursorPaginationResponse();
        
        // Assert
        Assert.Single(result.Items);
        Assert.Equal(response.NextCursor, result.NextCursor);
        Assert.Equal(response.HasNext, result.HasNext);
        Assert.Equal(books[0].Title, result.Items[0].Title);
    }
    
    [Fact]
    public void ToBookWithIssuanceRecordsResponse_ShouldMapCorrectly()
    {
        // Arrange
        var issuanceRecords = new List<BookBorrow>
        {
            new() 
            { 
                BorrowDate = new DateOnly(2024, 1, 1), 
                DueDate = new DateOnly(2024, 1, 31), 
                ReturnDate = new DateOnly(2024, 1, 15),
                Status = Enums.BookIssueStatus.Returned
            }
        };
        
        var book = new Book
        {
            Title = "Война и мир",
            Category = Enums.BookCategory.FictionBook,
            Authors = new List<string> { "Лев Толстой" },
            Description = "Великий роман",
            Year = 1869,
            Status = Enums.BookStatus.Borrow,
            IssuanceRecords = issuanceRecords
        };
        
        // Act
        var result = book.ToBookWithIssuanceRecordsResponse();
        
        // Assert
        Assert.Equal(book.Title, result.Title);
        Assert.Equal((Contracts.v1.Enums.BookCategory)book.Category, result.Category);
        Assert.Equal(book.Authors, result.Authors);
        Assert.Equal(book.Description, result.Description);
        Assert.Equal(book.Year, result.Year);
        Assert.Equal((BookStatus)book.Status, result.Status);
        Assert.Equal(book.IsArchived, result.IsArchived);
    }
    
    [Fact]
    public void ToIssuanceRecord_ShouldMapCorrectly()
    {
        // Arrange
        var bookBorrow = new BookBorrow
        {
            BorrowDate = new DateOnly(2024, 1, 1),
            DueDate = new DateOnly(2024, 1, 31),
            ReturnDate = new DateOnly(2024, 1, 15),
            Status = Enums.BookIssueStatus.Returned
        };
        
        // Act
        var result = bookBorrow.ToIssuanceRecord();
        
        // Assert
        Assert.Equal(bookBorrow.DueDate, result.DueDate);
        Assert.Equal(bookBorrow.ReturnDate, result.ReturnDate);
        Assert.Equal(bookBorrow.BorrowDate, result.BorrowDate);
    }
}