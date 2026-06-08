using FluentValidation.TestHelper;
using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Contracts.v1.Enums;
using PracticalWork.Library.Controllers.Validations.v1;

namespace PracticalWork.Library.Tests.Validation;

public class CreateBookRequestValidatorTests
{
    private readonly CreateBookRequestValidator _validator;
    
    public CreateBookRequestValidatorTests()
    {
        _validator = new CreateBookRequestValidator();
    }
    
    #region Title Tests
    
    [Fact]
    public void Validate_ValidTitle_ShouldNotHaveError()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Война и мир",
            Category: BookCategory.FictionBook,
            Authors: new List<string> { "Лев Толстой" },
            Description: "Описание",
            Year: 1869);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }
    
    [Fact]
    public void Validate_EmptyTitle_ShouldHaveError()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "",
            Category: BookCategory.FictionBook,
            Authors: new List<string> { "Автор" },
            Description: "Описание",
            Year: 2024);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Название книги обязательно.");
    }
    
    [Fact]
    public void Validate_TitleExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: new string('A', 501),
            Category: BookCategory.FictionBook,
            Authors: new List<string> { "Автор" },
            Description: "Описание",
            Year: 2024);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Название книги не может превышать 500 символов.");
    }
    
    #endregion
    
    #region Category Tests
    
    [Theory]
    [InlineData(BookCategory.FictionBook)]
    [InlineData(BookCategory.EducationalBook)]
    [InlineData(BookCategory.ScientificBook)]
    public void Validate_ValidCategory_ShouldNotHaveError(BookCategory category)
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Test Book",
            Category: category,
            Authors: new List<string> { "Author" },
            Description: "Описание",
            Year: 2024);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Category);
    }
    
    [Fact]
    public void Validate_InvalidCategory_ShouldHaveError()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Test Book",
            Category: (BookCategory)99,
            Authors: new List<string> { "Author" },
            Description: "Описание",
            Year: 2024);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Category)
            .WithErrorMessage("Категория должна быть от 1 до 3.");
    }
    
    #endregion
    
    #region Authors Tests
    
    [Fact]
    public void Validate_ValidAuthors_ShouldNotHaveError()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Test Book",
            Category: BookCategory.FictionBook,
            Authors: new List<string> { "Лев Толстой", "Достоевский" },
            Description: "Описание",
            Year: 2024);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Authors);
    }
    
    [Fact]
    public void Validate_EmptyAuthors_ShouldHaveError()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Test Book",
            Category: BookCategory.FictionBook,
            Authors: new List<string>(),
            Description: "Описание",
            Year: 2024);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Authors)
            .WithErrorMessage("Автор или авторы книги обязательны.");
    }
    
    [Fact]
    public void Validate_NullAuthors_ShouldHaveError()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Test Book",
            Category: BookCategory.FictionBook,
            Authors: null!,
            Description: "Описание",
            Year: 2024);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Authors)
            .WithErrorMessage("Автор или авторы книги обязательны.");
    }
    
    [Fact]
    public void Validate_AuthorsWithEmptyStrings_ShouldHaveError()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Test Book",
            Category: BookCategory.FictionBook,
            Authors: new List<string> { "Valid Author", "", "   " },
            Description: "Описание",
            Year: 2024);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Authors)
            .WithErrorMessage("Все авторы должны быть непустыми строками.");
    }
    
    #endregion
    
    #region Description Tests
    
    [Fact]
    public void Validate_DescriptionWithinLimit_ShouldNotHaveError()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Test Book",
            Category: BookCategory.FictionBook,
            Authors: new List<string> { "Author" },
            Description: new string('A', 2000),
            Year: 2024);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
    
    [Fact]
    public void Validate_DescriptionExceedsLimit_ShouldHaveError()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Test Book",
            Category: BookCategory.FictionBook,
            Authors: new List<string> { "Author" },
            Description: new string('A', 2001),
            Year: 2024);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Описание не может превышать 2000 символов.");
    }
    
    [Fact]
    public void Validate_NullDescription_ShouldNotHaveError()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Test Book",
            Category: BookCategory.FictionBook,
            Authors: new List<string> { "Author" },
            Description: null!,
            Year: 2024);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
    
    #endregion
    
    #region Year Tests
    
    [Theory]
    [InlineData(1800)]
    [InlineData(1900)]
    [InlineData(2000)]
    [InlineData(2024)]
    public void Validate_ValidYear_ShouldNotHaveError(int year)
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Test Book",
            Category: BookCategory.FictionBook,
            Authors: new List<string> { "Author" },
            Description: "Описание",
            Year: year);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }
    
    [Theory]
    [InlineData(1799)]
    [InlineData(1790)]
    public void Validate_InvalidYear_ShouldHaveError(int year)
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Test Book",
            Category: BookCategory.FictionBook,
            Authors: new List<string> { "Author" },
            Description: "Описание",
            Year: year);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Year)
            .WithErrorMessage("Год издания должен быть между 1800 и настоящем годом.");
    }
    
    #endregion
    
    #region Complex Validation Tests
    
    [Fact]
    public void Validate_ValidRequest_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "Война и мир",
            Category: BookCategory.FictionBook,
            Authors: new List<string> { "Лев Толстой" },
            Description: "Великий роман",
            Year: 1869);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public void Validate_MultipleErrors_ShouldHaveAllErrors()
    {
        // Arrange
        var request = new CreateBookRequest(
            Title: "",
            Category: (BookCategory)99,
            Authors: new List<string> { "", null! },
            Description: new string('A', 2001),
            Year: 1799);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.Category);
        result.ShouldHaveValidationErrorFor(x => x.Authors);
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }
    
    #endregion
}