using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Controllers.Validations.v1;

namespace PracticalWork.Library.Tests.Validation;

public class AddBookDetailsRequestValidatorTests
{
    private readonly AddBookDetailsRequestValidator _validator;
    
    public AddBookDetailsRequestValidatorTests()
    {
        _validator = new AddBookDetailsRequestValidator();
    }
    
    #region Description Tests
    
    [Fact]
    public void Validate_DescriptionWithinLimit_ShouldNotHaveError()
    {
        // Arrange
        var file = CreateMockFile("image.jpg", "image/jpeg", 1024);
        var request = new AddBookDetailsRequest(
            Id: Guid.NewGuid(),
            Description: new string('A', 2000),
            CoverImage: file);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
    
    [Fact]
    public void Validate_DescriptionExceedsLimit_ShouldHaveError()
    {
        // Arrange
        var file = CreateMockFile("image.jpg", "image/jpeg", 1024);
        var request = new AddBookDetailsRequest(
            Id: Guid.NewGuid(),
            Description: new string('A', 2001),
            CoverImage: file);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Описание не может превышать 2000 символов.");
    }
    
    [Fact]
    public void Validate_DescriptionNull_ShouldNotHaveError()
    {
        // Arrange
        var file = CreateMockFile("image.jpg", "image/jpeg", 1024);
        var request = new AddBookDetailsRequest(
            Id: Guid.NewGuid(),
            Description: null!,
            CoverImage: file);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
    
    [Fact]
    public void Validate_DescriptionEmpty_ShouldNotHaveError()
    {
        // Arrange
        var file = CreateMockFile("image.jpg", "image/jpeg", 1024);
        var request = new AddBookDetailsRequest(
            Id: Guid.NewGuid(),
            Description: string.Empty,
            CoverImage: file);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
    
    #endregion
    
    #region CoverImage Test
    
    [Fact]
    public void Validate_CoverImageTooLarge_ShouldHaveError()
    {
        // Arrange
        var file = CreateMockFile("image.jpg", "image/jpeg", 6 * 1024 * 1024);
        var request = new AddBookDetailsRequest(
            Id: Guid.NewGuid(),
            Description: "Test description",
            CoverImage: file);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CoverImage)
            .WithErrorMessage("Файл слишком большой(>5MB)");
    }
    
    [Theory]
    [InlineData("", "image/png")]
    [InlineData(null, "image/png")]
    public void Validate_CoverImageEmptyFileName_ShouldHaveError(string? fileName, string contentType)
    {
        // Arrange
        var file = CreateMockFile(fileName ?? "test.jpg", contentType, 1024);
        var request = new AddBookDetailsRequest(
            Id: Guid.NewGuid(),
            Description: "Test description",
            CoverImage: file);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CoverImage);
    }
    
    [Theory]
    [InlineData("image.bmp", "image/bmp")]
    [InlineData("image.gif", "image/gif")]
    [InlineData("image.svg", "image/svg+xml")]
    [InlineData("image.tiff", "image/tiff")]
    public void Validate_InvalidCoverImageExtension_ShouldHaveError(string fileName, string contentType)
    {
        // Arrange
        var file = CreateMockFile(fileName, contentType, 1024);
        var request = new AddBookDetailsRequest(
            Id: Guid.NewGuid(),
            Description: "Test description",
            CoverImage: file);
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CoverImage)
            .WithErrorMessage("Обложка должна быть формата jpeg/jpg/png/webp");
    }
    
    #endregion
    
    #region Helper Methods
    
    private static IFormFile CreateMockFile(string fileName, string contentType, long length)
    {
        var stream = new MemoryStream();
        var file = new FormFile(stream, 0, length, fileName, fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
        return file;
    }
    
    #endregion
}