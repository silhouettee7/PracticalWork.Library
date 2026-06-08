using FluentValidation.TestHelper;
using PracticalWork.Library.Contracts.v1.Reader.Request;
using PracticalWork.Library.Controllers.Validations.v1;

namespace PracticalWork.Library.Tests.Validation;

public class ExtendReaderExpiryDateRequestValidatorTests
{
    private readonly ExtendReaderExpiryDateRequestValidator _validator;
    
    public ExtendReaderExpiryDateRequestValidatorTests()
    {
        _validator = new ExtendReaderExpiryDateRequestValidator();
    }
    
    [Fact]
    public void Validate_FutureDate_ShouldNotHaveError()
    {
        // Arrange
        var request = new ExtendReaderExpiryDateRequest(
            Date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Date);
    }
    
    [Fact]
    public void Validate_FarFutureDate_ShouldNotHaveError()
    {
        // Arrange
        var request = new ExtendReaderExpiryDateRequest(
            Date: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)));
        
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Date);
    }
}