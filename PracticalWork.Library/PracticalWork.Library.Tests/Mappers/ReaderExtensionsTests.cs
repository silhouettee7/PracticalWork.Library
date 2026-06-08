using PracticalWork.Library.Contracts.v1.Reader.Request;
using PracticalWork.Library.Controllers.Mappers.v1;

namespace PracticalWork.Library.Tests.Mappers;

public class ReaderExtensionsTests
{
    [Fact]
    public void ToReader_FromCreateReaderRequest_ShouldMapCorrectly()
    {
        // Arrange
        var expiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));
        var request = new CreateReaderRequest(
            FullName: "Иван Иванович Иванов",
            PhoneNumber: "+79991234567",
            ExpiryDate: expiryDate);
        
        // Act
        var result = request.ToReader();
        
        // Assert
        Assert.Equal(request.FullName, result.FullName);
        Assert.Equal(request.PhoneNumber, result.PhoneNumber);
        Assert.Equal(request.ExpiryDate, result.ExpiryDate);
    }
}