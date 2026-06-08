using FluentValidation.TestHelper;
using PracticalWork.Library.Contracts.v1.Reader.Request;
using PracticalWork.Library.Controllers.Validations.v1;

namespace PracticalWork.Library.Tests.Validation;

public class CreateReaderRequestValidatorTests
{
    private readonly CreateReaderRequestValidator _validator;
    
    public CreateReaderRequestValidatorTests()
    {
        _validator = new CreateReaderRequestValidator();
    }
    
    #region PhoneNumber Tests
    
    [Theory]
    [InlineData("+79991234567")]
    [InlineData("79991234567")]
    [InlineData("+7 999 123 45 67")]
    [InlineData("+7-999-123-45-67")]
    [InlineData("+7(999)1234567")]
    [InlineData("8-999-123-45-67")]
    public void Validate_ValidPhoneNumber_ShouldNotHaveError(string phoneNumber)
    {
        // Arrange
        var request = new CreateReaderRequest(
            FullName: "Иван Иванов",
            PhoneNumber: phoneNumber,
            ExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("123")]
    [InlineData("+7999123456")]
    [InlineData("abc")]
    [InlineData("+7(999)123-45")]
    public void Validate_InvalidPhoneNumber_ShouldHaveError(string? phoneNumber)
    {
        // Arrange
        var request = new CreateReaderRequest(
            FullName: "Иван Иванов",
            PhoneNumber: phoneNumber!,
            ExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("Неправильный формат телефона");
    }
    
    #endregion
    
    #region FullName Tests
    
    [Fact]
    public void Validate_ValidFullName_ShouldNotHaveError()
    {
        // Arrange
        var request = new CreateReaderRequest(
            FullName: "Иван Иванович Иванов",
            PhoneNumber: "+79991234567",
            ExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyFullName_ShouldHaveError(string? fullName)
    {
        // Arrange
        var request = new CreateReaderRequest(
            FullName: fullName!,
            PhoneNumber: "+79991234567",
            ExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("ФИО обязательно");
    }
    
    [Fact]
    public void Validate_FullNameExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var request = new CreateReaderRequest(
            FullName: new string('A', 51),
            PhoneNumber: "+79991234567",
            ExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Имя не должно превышать 50 символов");
    }
    
    #endregion
    
    #region ExpiryDate Tests
    
    [Fact]
    public void Validate_FutureExpiryDate_ShouldNotHaveError()
    {
        // Arrange
        var request = new CreateReaderRequest(
            FullName: "Иван Иванов",
            PhoneNumber: "+79991234567",
            ExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiryDate);
    }
    
    #endregion
    
    #region Complex Validation Tests
    
    [Fact]
    public void Validate_ValidRequest_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var request = new CreateReaderRequest(
            FullName: "Иван Иванович Иванов",
            PhoneNumber: "+79991234567",
            ExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public void Validate_MultipleErrors_ShouldHaveAllErrors()
    {
        // Arrange
        var request = new CreateReaderRequest(
            FullName: "",
            PhoneNumber: "invalid",
            ExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
        result.ShouldHaveValidationErrorFor(x => x.ExpiryDate);
    }
    
    #endregion
}