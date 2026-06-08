using AutoFixture;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Tests.Domain;

public class EmailMessageTests
{
    private readonly Fixture _fixture = new();
    
    public EmailMessageTests()
    {
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
    
    #region Constructor Tests
    
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var emailTo = "user@example.com";
        var subject = "Test Subject";
        var body = "<html>Test Body</html>";
        var isBodyHtml = true;
        
        // Act
        var email = new EmailMessage(emailTo, subject, body, isBodyHtml);
        
        // Assert
        Assert.Equal(emailTo, email.EmailTo);
        Assert.Equal(subject, email.Subject);
        Assert.Equal(body, email.Body);
        Assert.True(email.IsBodyHtml);
        Assert.Equal("UTF-8", email.BodyEncoding);
        Assert.Equal("UTF-8", email.SubjectEncoding);
    }
    
    [Fact]
    public void Constructor_WhenIsBodyHtmlFalse_ShouldSetIsBodyHtmlToFalse()
    {
        // Arrange & Act
        var email = new EmailMessage("test@test.com", "Subject", "Plain text body", false);
        
        // Assert
        Assert.False(email.IsBodyHtml);
    }
    
    #endregion
    
    #region Personalize Tests
    
    [Fact]
    public void Personalize_WhenIsBodyHtmlTrue_ShouldReplaceAllProperties()
    {
        // Arrange
        var template = "Hello {{FullName}}, your order {{OrderId}} is ready. Books: {{Books}}";
        var email = new EmailMessage("user@test.com", "Test", template, true);
        
        var personalizationObject = new
        {
            FullName = "John Doe",
            OrderId = 12345,
            Books = new List<string> { "Book 1", "Book 2", "Book 3" }
        };
        
        // Act
        email.Personalize(personalizationObject);
        
        // Assert
        Assert.Contains("Hello John Doe", email.Body);
        Assert.Contains("your order 12345 is ready", email.Body);
        Assert.Contains("Books: Book 1, Book 2, Book 3", email.Body);
        Assert.DoesNotContain("{{FullName}}", email.Body);
        Assert.DoesNotContain("{{OrderId}}", email.Body);
        Assert.DoesNotContain("{{Books}}", email.Body);
    }
    
    [Fact]
    public void Personalize_WhenIsBodyHtmlFalse_ShouldNotReplaceAnything()
    {
        // Arrange
        var template = "Hello {{FullName}}";
        var email = new EmailMessage("user@test.com", "Test", template, false);
        
        var personalizationObject = new { FullName = "John Doe" };
        
        // Act
        email.Personalize(personalizationObject);
        
        // Assert
        Assert.Equal(template, email.Body);
        Assert.Contains("{{FullName}}", email.Body);
    }
    
    [Fact]
    public void Personalize_WithDateOnlyProperty_ShouldFormatAsDD_MM_YYYY()
    {
        // Arrange
        var template = "Event date: {{EventDate}}";
        var email = new EmailMessage("user@test.com", "Test", template, true);
        
        var personalizationObject = new
        {
            EventDate = new DateOnly(2024, 12, 25)
        };
        
        // Act
        email.Personalize(personalizationObject);
        
        // Assert
        Assert.Equal("Event date: 25-12-2024", email.Body);
    }
    
    [Fact]
    public void Personalize_WithNullPropertyValue_ShouldReplaceWithEmptyString()
    {
        // Arrange
        var template = "Name: {{Name}}, Age: {{Age}}";
        var email = new EmailMessage("user@test.com", "Test", template, true);
        
        var personalizationObject = new
        {
            Name = "John Doe",
            Age = (int?)null
        };
        
        // Act
        email.Personalize(personalizationObject);
        
        // Assert
        Assert.Equal("Name: John Doe, Age: ", email.Body);
    }
    
    [Fact]
    public void Personalize_WithEmptyCollection_ShouldReplaceWithEmptyString()
    {
        // Arrange
        var template = "Items: {{Items}}";
        var email = new EmailMessage("user@test.com", "Test", template, true);
        
        var personalizationObject = new
        {
            Items = new List<string>()
        };
        
        // Act
        email.Personalize(personalizationObject);
        
        // Assert
        Assert.Equal("Items: ", email.Body);
    }
    
    [Fact]
    public void Personalize_WithMultiplePlaceholders_ShouldReplaceAllOccurrences()
    {
        // Arrange
        var template = "Hello {{Name}}, {{Name}} is your name. Welcome {{Name}}!";
        var email = new EmailMessage("user@test.com", "Test", template, true);
        
        var personalizationObject = new { Name = "John" };
        
        // Act
        email.Personalize(personalizationObject);
        
        // Assert
        Assert.Equal("Hello John, John is your name. Welcome John!", email.Body);
    }
    
    [Fact]
    public void Personalize_WithNestedObject_ShouldNotReplaceNestedProperties()
    {
        // Arrange
        var template = "User: {{User.Name}}";
        var email = new EmailMessage("user@test.com", "Test", template, true);
        
        var personalizationObject = new
        {
            User = new { Name = "John Doe" }
        };
        
        // Act
        email.Personalize(personalizationObject);
        
        // Assert
        // Nested properties are not supported, should remain as is
        Assert.Contains("{{User.Name}}", email.Body);
    }
    
    [Fact]
    public void Personalize_ShouldPreserveNonPlaceholderText()
    {
        // Arrange
        var template = "Hello {{Name}}! How are you today?";
        var email = new EmailMessage("user@test.com", "Test", template, true);
        
        var personalizationObject = new { Name = "John" };
        
        // Act
        email.Personalize(personalizationObject);
        
        // Assert
        Assert.Equal("Hello John! How are you today?", email.Body);
    }
    
    #endregion
    
    #region Encoding Properties Tests
    
    [Fact]
    public void BodyEncoding_ShouldBeSettable()
    {
        // Arrange
        var email = new EmailMessage("test@test.com", "Subject", "Body", true);
        
        // Act
        email.BodyEncoding = "ISO-8859-1";
        
        // Assert
        Assert.Equal("ISO-8859-1", email.BodyEncoding);
    }
    
    [Fact]
    public void SubjectEncoding_ShouldBeSettable()
    {
        // Arrange
        var email = new EmailMessage("test@test.com", "Subject", "Body", true);
        
        // Act
        email.SubjectEncoding = "ISO-8859-1";
        
        // Assert
        Assert.Equal("ISO-8859-1", email.SubjectEncoding);
    }
    
    #endregion
}