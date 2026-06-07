using AutoFixture;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Application.Services;
using PracticalWork.Library.Dtos;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Tests.JobServices;

public class NotificationServiceTests
{
    private readonly Mock<IBorrowRepository> _borrowRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IEmailMessageTemplateService> _emailMessageTemplateServiceMock;
    private readonly Fixture _fixture = new();
    private readonly NotificationService _notificationService;
    
    private readonly string _notificationTemplateFileName;
    private readonly string _notificationSubject;
    private readonly int _notificationToleranceMinutes;
    private readonly DateTimeOffset _fixedDateTimeProvider;
    
    public NotificationServiceTests()
    {
        _borrowRepositoryMock = new Mock<IBorrowRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _emailMessageTemplateServiceMock = new Mock<IEmailMessageTemplateService>();
        Mock<ILogger<NotificationService>> loggerMock = new();
        
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        _fixedDateTimeProvider = new DateTimeOffset(2024, 01, 15, 10, 30, 00, TimeSpan.Zero);
        var timeProviderMock = new Mock<TimeProvider>();
        timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(_fixedDateTimeProvider);

        _notificationTemplateFileName = "notification-template.html";
        _notificationSubject = "Book Borrowed Notification";
        
        var emailMessagesOptions = new EmailMessagesOptions
        {
            Notification = new EmailInfo
            {
                TemplateFileName = _notificationTemplateFileName,
                Subject = _notificationSubject
            }
        };
        var emailMessagesOptionsMonitor = new Mock<IOptionsMonitor<EmailMessagesOptions>>();
        emailMessagesOptionsMonitor.Setup(x => x.CurrentValue).Returns(emailMessagesOptions);

        _notificationToleranceMinutes = 30;
        var schedulerOptions = new SchedulerOptions
        {
            NotificationToleranceMinutes = _notificationToleranceMinutes
        };
        var schedulerOptionsMonitor = new Mock<IOptionsMonitor<SchedulerOptions>>();
        schedulerOptionsMonitor.Setup(x => x.CurrentValue).Returns(schedulerOptions);
        
        _notificationService = new NotificationService(
            _borrowRepositoryMock.Object,
            _emailServiceMock.Object,
            loggerMock.Object,
            timeProviderMock.Object,
            _emailMessageTemplateServiceMock.Object,
            emailMessagesOptionsMonitor.Object,
            schedulerOptionsMonitor.Object);
    }
    
    [Fact]
    public async Task NotifyReadersWithIssuedBorrowedBooksAsync_Success_ShouldNotifyAllReadersAndUpdateLastEmailSent()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var borrowedBooksInfo = _fixture
            .Build<BorrowedIssuedBookInfoDto>()
            .Without(x => x.DueDate)
            .CreateMany(3).ToList();
        var htmlTemplate = _fixture.Create<string>();
        
        var expectedDateThreeDaysAfter = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(3));
        var expectedDateOneDayAgo = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(-1));
        var expectedToleranceMinutesAgo = _fixedDateTimeProvider.DateTime.AddMinutes(-_notificationToleranceMinutes);
        
        _borrowRepositoryMock
            .Setup(x => x.GetBorrowedIssuedBooksInfo(
                expectedDateOneDayAgo, 
                expectedDateThreeDaysAfter, 
                expectedToleranceMinutesAgo, 
                cancellationToken))
            .ReturnsAsync(borrowedBooksInfo);
        
        _emailMessageTemplateServiceMock
            .Setup(x => x.GetEmailMessageHtmlBodyTemplateAsync(_notificationTemplateFileName, cancellationToken))
            .ReturnsAsync(htmlTemplate);
        
        // Act
        await _notificationService.NotifyReadersWithIssuedBorrowedBooksAsync(cancellationToken);
        
        // Assert
        foreach (var borrowBook in borrowedBooksInfo)
        {
            _emailServiceMock.Verify(x => x.NotifyReaderAboutBorrowedBookAsync(
                borrowBook.ReaderFullName,
                It.Is<BorrowedBookNotification>(m => 
                    m.BookTitle == borrowBook.BookTitle &&
                    m.ReaderFullName == borrowBook.ReaderFullName &&
                    m.Authors.SequenceEqual(borrowBook.Authors) && 
                    m.ReturnDate == borrowBook.DueDate),
                _notificationSubject,
                htmlTemplate,
                cancellationToken), Times.Once);
            
            _borrowRepositoryMock.Verify(x => x.UpdateLastEmailSentAsync(
                borrowBook.Id,
                _fixedDateTimeProvider.DateTime,
                cancellationToken), Times.Once);
        }
    }
    
    [Fact]
    public async Task NotifyReadersWithIssuedBorrowedBooksAsync_NoBorrowedBooks_ShouldNotNotifyOrUpdate()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var emptyBorrowedBooksInfo = new List<BorrowedIssuedBookInfoDto>();
        var htmlTemplate = _fixture.Create<string>();
        
        var expectedDateThreeDaysAfter = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(3));
        var expectedDateOneDayAgo = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(-1));
        var expectedToleranceMinutesAgo = _fixedDateTimeProvider.DateTime.AddMinutes(-_notificationToleranceMinutes);
        
        _borrowRepositoryMock
            .Setup(x => x.GetBorrowedIssuedBooksInfo(
                expectedDateOneDayAgo, 
                expectedDateThreeDaysAfter, 
                expectedToleranceMinutesAgo, 
                cancellationToken))
            .ReturnsAsync(emptyBorrowedBooksInfo);
        
        _emailMessageTemplateServiceMock
            .Setup(x => x.GetEmailMessageHtmlBodyTemplateAsync(_notificationTemplateFileName, cancellationToken))
            .ReturnsAsync(htmlTemplate);
        
        // Act
        await _notificationService.NotifyReadersWithIssuedBorrowedBooksAsync(cancellationToken);
        
        // Assert
        _emailServiceMock.Verify(x => x.NotifyReaderAboutBorrowedBookAsync(
            It.IsAny<string>(),
            It.IsAny<BorrowedBookNotification>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            cancellationToken), Times.Never);
        
        _borrowRepositoryMock.Verify(x => x.UpdateLastEmailSentAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            cancellationToken), Times.Never);
    }
    
    [Fact]
    public async Task NotifyReadersWithIssuedBorrowedBooksAsync_WhenUpdateLastEmailSentFails_ShouldContinueSendingEmails()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var borrowedBooksInfo = _fixture
            .Build<BorrowedIssuedBookInfoDto>()
            .Without(x => x.DueDate)
            .CreateMany(2).ToList();
        var htmlTemplate = _fixture.Create<string>();
        
        var expectedDateThreeDaysAfter = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(3));
        var expectedDateOneDayAgo = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(-1));
        var expectedToleranceMinutesAgo = _fixedDateTimeProvider.DateTime.AddMinutes(-_notificationToleranceMinutes);
        
        _borrowRepositoryMock
            .Setup(x => x.GetBorrowedIssuedBooksInfo(
                expectedDateOneDayAgo, 
                expectedDateThreeDaysAfter, 
                expectedToleranceMinutesAgo, 
                cancellationToken))
            .ReturnsAsync(borrowedBooksInfo);
        
        _emailMessageTemplateServiceMock
            .Setup(x => x.GetEmailMessageHtmlBodyTemplateAsync(_notificationTemplateFileName, cancellationToken))
            .ReturnsAsync(htmlTemplate);
        
        var exception = new Exception("Database error");
        _borrowRepositoryMock
            .Setup(x => x.UpdateLastEmailSentAsync(
                borrowedBooksInfo[0].Id,
                _fixedDateTimeProvider.DateTime,
                cancellationToken))
            .ThrowsAsync(exception);
        
        // Act
        await _notificationService.NotifyReadersWithIssuedBorrowedBooksAsync(cancellationToken);
        
        // Assert
        _emailServiceMock.Verify(x => x.NotifyReaderAboutBorrowedBookAsync(
            borrowedBooksInfo[0].ReaderFullName,
            It.Is<BorrowedBookNotification>(m => 
                m.BookTitle == borrowedBooksInfo[0].BookTitle &&
                m.ReaderFullName == borrowedBooksInfo[0].ReaderFullName &&
                m.Authors.SequenceEqual(borrowedBooksInfo[0].Authors) && 
                m.ReturnDate == borrowedBooksInfo[0].DueDate),
            _notificationSubject,
            htmlTemplate,
            cancellationToken), Times.Once);
        
        // Второй читатель должен быть обработан не смотря на ошибку первого
        _emailServiceMock.Verify(x => x.NotifyReaderAboutBorrowedBookAsync(
            borrowedBooksInfo[1].ReaderFullName,
            It.Is<BorrowedBookNotification>(m => 
                m.BookTitle == borrowedBooksInfo[1].BookTitle &&
                m.ReaderFullName == borrowedBooksInfo[1].ReaderFullName &&
                m.Authors.SequenceEqual(borrowedBooksInfo[1].Authors) && 
                m.ReturnDate == borrowedBooksInfo[1].DueDate),
            _notificationSubject,
            htmlTemplate,
            cancellationToken), Times.Once);
        
        _borrowRepositoryMock.Verify(x => x.UpdateLastEmailSentAsync(
            borrowedBooksInfo[1].Id,
            _fixedDateTimeProvider.DateTime,
            cancellationToken), Times.Once);
    }
    
    [Fact]
    public async Task NotifyReadersWithIssuedBorrowedBooksAsync_WhenEmailServiceThrows_ShouldStillUpdateLastEmailSent()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var borrowedBooksInfo = _fixture
            .Build<BorrowedIssuedBookInfoDto>()
            .Without(x => x.DueDate)
            .CreateMany(2).ToList();
        var htmlTemplate = _fixture.Create<string>();
        
        var expectedDateThreeDaysAfter = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(3));
        var expectedDateOneDayAgo = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(-1));
        var expectedToleranceMinutesAgo = _fixedDateTimeProvider.DateTime.AddMinutes(-_notificationToleranceMinutes);
        
        _borrowRepositoryMock
            .Setup(x => x.GetBorrowedIssuedBooksInfo(
                expectedDateOneDayAgo, 
                expectedDateThreeDaysAfter, 
                expectedToleranceMinutesAgo, 
                cancellationToken))
            .ReturnsAsync(borrowedBooksInfo);
        
        _emailMessageTemplateServiceMock
            .Setup(x => x.GetEmailMessageHtmlBodyTemplateAsync(_notificationTemplateFileName, cancellationToken))
            .ReturnsAsync(htmlTemplate);
        
        var emailException = new Exception("Email service error");
        _emailServiceMock
            .Setup(x => x.NotifyReaderAboutBorrowedBookAsync(
                borrowedBooksInfo[0].ReaderFullName,
                It.Is<BorrowedBookNotification>(m => 
                    m.BookTitle == borrowedBooksInfo[0].BookTitle &&
                    m.ReaderFullName == borrowedBooksInfo[0].ReaderFullName &&
                    m.Authors.SequenceEqual(borrowedBooksInfo[0].Authors) && 
                    m.ReturnDate == borrowedBooksInfo[0].DueDate),
                _notificationSubject,
                htmlTemplate,
                cancellationToken))
            .ThrowsAsync(emailException);
        
        // Act
        await _notificationService.NotifyReadersWithIssuedBorrowedBooksAsync(cancellationToken);
        
        // Assert
        
        // LastEmailSent все равно должен обновиться
        _borrowRepositoryMock.Verify(x => x.UpdateLastEmailSentAsync(
            borrowedBooksInfo[0].Id,
            _fixedDateTimeProvider.DateTime,
            cancellationToken), Times.Once);
        
        // Второй читатель должен быть обработан
        _emailServiceMock.Verify(x => x.NotifyReaderAboutBorrowedBookAsync(
            borrowedBooksInfo[1].ReaderFullName,
            It.Is<BorrowedBookNotification>(m => 
                m.BookTitle == borrowedBooksInfo[1].BookTitle &&
                m.ReaderFullName == borrowedBooksInfo[1].ReaderFullName &&
                m.Authors.SequenceEqual(borrowedBooksInfo[1].Authors) && 
                m.ReturnDate == borrowedBooksInfo[1].DueDate),
            _notificationSubject,
            htmlTemplate,
            cancellationToken), Times.Once);
        
        _borrowRepositoryMock.Verify(x => x.UpdateLastEmailSentAsync(
            borrowedBooksInfo[1].Id,
            _fixedDateTimeProvider.DateTime,
            cancellationToken), Times.Once);
    }
    
    [Fact]
    public async Task NotifyReadersWithIssuedBorrowedBooksAsync_WhenTemplateServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var borrowedBooksInfo = _fixture
            .Build<BorrowedIssuedBookInfoDto>()
            .Without(x => x.DueDate)
            .CreateMany(3).ToList();
        
        var expectedDateThreeDaysAfter = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(3));
        var expectedDateOneDayAgo = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(-1));
        var expectedToleranceMinutesAgo = _fixedDateTimeProvider.DateTime.AddMinutes(-_notificationToleranceMinutes);
        
        _borrowRepositoryMock
            .Setup(x => x.GetBorrowedIssuedBooksInfo(
                expectedDateOneDayAgo, 
                expectedDateThreeDaysAfter, 
                expectedToleranceMinutesAgo, 
                cancellationToken))
            .ReturnsAsync(borrowedBooksInfo);
        
        var templateException = new FileNotFoundException("Template not found");
        _emailMessageTemplateServiceMock
            .Setup(x => x.GetEmailMessageHtmlBodyTemplateAsync(_notificationTemplateFileName, cancellationToken))
            .ThrowsAsync(templateException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => _notificationService.NotifyReadersWithIssuedBorrowedBooksAsync(cancellationToken));
        
        Assert.Equal("Template not found", exception.Message);
        
        _emailServiceMock.Verify(x => x.NotifyReaderAboutBorrowedBookAsync(
            It.IsAny<string>(),
            It.IsAny<BorrowedBookNotification>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            cancellationToken), Times.Never);
        
        _borrowRepositoryMock.Verify(x => x.UpdateLastEmailSentAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            cancellationToken), Times.Never);
    }
    
    [Fact]
    public async Task NotifyReadersWithIssuedBorrowedBooksAsync_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedDateThreeDaysAfter = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(3));
        var expectedDateOneDayAgo = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddDays(-1));
        var expectedToleranceMinutesAgo = _fixedDateTimeProvider.DateTime.AddMinutes(-_notificationToleranceMinutes);
        
        var dbException = new Exception("Database connection error");
        _borrowRepositoryMock
            .Setup(x => x.GetBorrowedIssuedBooksInfo(
                expectedDateOneDayAgo, 
                expectedDateThreeDaysAfter, 
                expectedToleranceMinutesAgo, 
                cancellationToken))
            .ThrowsAsync(dbException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _notificationService.NotifyReadersWithIssuedBorrowedBooksAsync(cancellationToken));
        
        Assert.Equal("Database connection error", exception.Message);
        
        _emailMessageTemplateServiceMock.Verify(x => x.GetEmailMessageHtmlBodyTemplateAsync(
            It.IsAny<string>(), cancellationToken), Times.Never);
        
        _emailServiceMock.Verify(x => x.NotifyReaderAboutBorrowedBookAsync(
            It.IsAny<string>(),
            It.IsAny<BorrowedBookNotification>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            cancellationToken), Times.Never);
        
        _borrowRepositoryMock.Verify(x => x.UpdateLastEmailSentAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            cancellationToken), Times.Never);
    }
}