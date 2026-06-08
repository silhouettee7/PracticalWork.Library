using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Data.PostgreSql.Extensions;
using PracticalWork.Library.Enums;
using BookStatus = PracticalWork.Library.Enums.BookStatus;

namespace PracticalWork.Library.Tests.Mappers;

public class MapEntitiesExtTests
{
    [Fact]
    public void ToBook_FromEducationalBookEntity_ShouldMapCorrectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var issuanceRecords = new List<BookBorrowEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Status = BookIssueStatus.Issued,
                BorrowDate = new DateOnly(2024, 1, 1),
                DueDate = new DateOnly(2024, 1, 31),
                ReturnDate = null,
                Book = new EducationalBookEntity()
            }
        };
        
        var educationalBookEntity = new EducationalBookEntity
        {
            Id = entityId,
            Title = "Математика 6 класс",
            Authors = new List<string> { "И.И. Иванов", "П.П. Петров" },
            Description = "Учебник по математике",
            Year = 2024,
            Category = BookCategory.EducationalBook,
            Status = BookStatus.Available,
            CoverImagePath = "covers/2024/01/math.jpg",
            Subject = "Математика",
            GradeLevel = "6 класс",
            IssuanceRecords = issuanceRecords
        };
        
        // Act
        var result = educationalBookEntity.ToBook();
        
        // Assert
        Assert.Equal(educationalBookEntity.Title, result.Title);
        Assert.Equal(educationalBookEntity.Authors, result.Authors);
        Assert.Equal(educationalBookEntity.Description, result.Description);
        Assert.Equal(educationalBookEntity.Year, result.Year);
        Assert.Equal(educationalBookEntity.Category, result.Category);
        Assert.Equal(educationalBookEntity.Status, result.Status);
        Assert.Equal(educationalBookEntity.CoverImagePath, result.CoverImagePath);
        Assert.Equal(entityId, result.Cursor.Id);
        Assert.Single(result.IssuanceRecords);
    }
    
    [Fact]
    public void ToBook_FromFictionBookEntity_ShouldMapCorrectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var fictionBookEntity = new FictionBookEntity
        {
            Id = entityId,
            Title = "Война и мир",
            Authors = new List<string> { "Лев Толстой" },
            Description = "Роман-эпопея",
            Year = 1869,
            Category = BookCategory.FictionBook,
            Status = BookStatus.Available,
            CoverImagePath = "covers/2024/01/war_and_peace.jpg",
            CategoriesOfFiction = "Роман, Эпопея",
            IssuanceRecords = new List<BookBorrowEntity>()
        };
        
        // Act
        var result = fictionBookEntity.ToBook();
        
        // Assert
        Assert.Equal(fictionBookEntity.Title, result.Title);
        Assert.Equal(fictionBookEntity.Authors, result.Authors);
        Assert.Equal(fictionBookEntity.Description, result.Description);
        Assert.Equal(fictionBookEntity.Year, result.Year);
        Assert.Equal(fictionBookEntity.Category, result.Category);
        Assert.Equal(fictionBookEntity.Status, result.Status);
        Assert.Equal(fictionBookEntity.CoverImagePath, result.CoverImagePath);
        Assert.Equal(entityId, result.Cursor.Id);
        Assert.Empty(result.IssuanceRecords);
    }
    
    [Fact]
    public void ToBook_FromScientificBookEntity_ShouldMapCorrectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var scientificBookEntity = new ScientificBookEntity
        {
            Id = entityId,
            Title = "Квантовая физика",
            Authors = new List<string> { "Р. Фейнман" },
            Description = "Основы квантовой механики",
            Year = 1965,
            Category = BookCategory.ScientificBook,
            Status = BookStatus.Available,
            CoverImagePath = "covers/2024/01/quantum.jpg",
            ResearchField = "Физика",
            Publisher = "Наука",
            IssuanceRecords = new List<BookBorrowEntity>()
        };
        
        // Act
        var result = scientificBookEntity.ToBook();
        
        // Assert
        Assert.Equal(scientificBookEntity.Title, result.Title);
        Assert.Equal(scientificBookEntity.Authors, result.Authors);
        Assert.Equal(scientificBookEntity.Description, result.Description);
        Assert.Equal(scientificBookEntity.Year, result.Year);
        Assert.Equal(scientificBookEntity.Category, result.Category);
        Assert.Equal(scientificBookEntity.Status, result.Status);
        Assert.Equal(scientificBookEntity.CoverImagePath, result.CoverImagePath);
        Assert.Equal(entityId, result.Cursor.Id);
        Assert.Empty(result.IssuanceRecords);
    }
    
    [Fact]
    public void ToBook_FromAbstractBookEntityWithNullIssuanceRecords_ShouldMapEmptyList()
    {
        // Arrange
        var educationalBookEntity = new EducationalBookEntity
        {
            Id = Guid.NewGuid(),
            Title = "Книга без выдач",
            Authors = new List<string> { "Автор" },
            Description = "Описание",
            Year = 2024,
            Category = BookCategory.EducationalBook,
            Status = BookStatus.Available,
            IssuanceRecords = null
        };
        
        // Act
        var result = educationalBookEntity.ToBook();
        
        // Assert
        Assert.NotNull(result.IssuanceRecords);
        Assert.Empty(result.IssuanceRecords);
    }
    
    [Fact]
    public void ToBook_FromAbstractBookEntityWithEmptyIssuanceRecords_ShouldMapEmptyList()
    {
        // Arrange
        var fictionBookEntity = new FictionBookEntity
        {
            Id = Guid.NewGuid(),
            Title = "Книга без выдач",
            Authors = new List<string> { "Автор" },
            Description = "Описание",
            Year = 2024,
            Category = BookCategory.FictionBook,
            Status = BookStatus.Available,
            IssuanceRecords = new List<BookBorrowEntity>()
        };
        
        // Act
        var result = fictionBookEntity.ToBook();
        
        // Assert
        Assert.NotNull(result.IssuanceRecords);
        Assert.Empty(result.IssuanceRecords);
    }
    
    [Fact]
    public void ToBookBorrow_FromBookBorrowEntity_ShouldMapCorrectly()
    {
        // Arrange
        var borrowDate = new DateOnly(2024, 1, 1);
        var dueDate = new DateOnly(2024, 1, 31);
        var returnDate = new DateOnly(2024, 1, 15);
        
        var bookEntity = new EducationalBookEntity
        {
            Id = Guid.NewGuid(),
            Title = "Тестовая книга",
            Status = BookStatus.Borrow,
            Subject = "Математика",
            GradeLevel = "6 класс"
        };
        
        var entity = new BookBorrowEntity
        {
            Id = Guid.NewGuid(),
            Status = BookIssueStatus.Returned,
            BorrowDate = borrowDate,
            DueDate = dueDate,
            ReturnDate = returnDate,
            Book = bookEntity
        };
        
        // Act
        var result = entity.ToBookBorrow();
        
        // Assert
        Assert.Equal(entity.Status, result.Status);
        Assert.Equal(entity.BorrowDate, result.BorrowDate);
        Assert.Equal(entity.DueDate, result.DueDate);
        Assert.Equal(entity.ReturnDate, result.ReturnDate);
        Assert.Equal(entity.Book.Status, result.Book.Status);
    }
    
    [Fact]
    public void ToBookBorrow_FromBookBorrowEntityWithNullReturnDate_ShouldMapDefaultReturnDate()
    {
        // Arrange
        var borrowDate = new DateOnly(2024, 1, 1);
        var dueDate = new DateOnly(2024, 1, 31);
        
        var bookEntity = new ScientificBookEntity
        {
            Id = Guid.NewGuid(),
            Title = "Тестовая книга",
            Status = BookStatus.Borrow,
            ResearchField = "Физика",
            Publisher = "Наука"
        };
        
        var entity = new BookBorrowEntity
        {
            Id = Guid.NewGuid(),
            Status = BookIssueStatus.Issued,
            BorrowDate = borrowDate,
            DueDate = dueDate,
            ReturnDate = null,
            Book = bookEntity
        };
        
        // Act
        var result = entity.ToBookBorrow();
        
        // Assert
        Assert.Equal(entity.Status, result.Status);
        Assert.Equal(entity.BorrowDate, result.BorrowDate);
        Assert.Equal(entity.DueDate, result.DueDate);
        Assert.Equal(default(DateOnly), result.ReturnDate);
        Assert.Equal(entity.Book.Status, result.Book.Status);
    }
    
    [Fact]
    public void ToBook_ShouldPreserveCursorIdFromEntityId()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var educationalBookEntity = new EducationalBookEntity
        {
            Id = entityId,
            Title = "Тест",
            Authors = new List<string> { "Автор" },
            Description = "Описание",
            Year = 2024,
            Category = BookCategory.EducationalBook,
            Status = BookStatus.Available,
            Subject = "Математика",
            GradeLevel = "6 класс"
        };
        
        // Act
        var result = educationalBookEntity.ToBook();
        
        // Assert
        Assert.Equal(entityId, result.Cursor.Id);
    }
}