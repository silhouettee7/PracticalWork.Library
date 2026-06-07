using AutoFixture;
using Domain.Abstractions.Services;
using Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Application.Services;
using PracticalWork.Library.Dtos;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Tests.JobServices;

public class AdministrationReportServiceTests
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IBorrowRepository> _borrowRepositoryMock;
    private readonly Mock<IReaderRepository> _readerRepositoryMock;
    private readonly Mock<IAdministrationReportRepository> _administrationReportRepositoryMock;
    private readonly Mock<IReportGenerateService> _reportGenerateServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IEmailMessageTemplateService> _emailMessageTemplateServiceMock;
    private readonly Fixture _fixture = new();
    private readonly AdministrationReportService _administrationReportService;

    private readonly string _reportsAdministrationBucketName;
    private List<string> _adminEmails;
    private readonly string _reportTemplateFileName;
    private readonly string _reportSubject;
    private readonly string _reportName;
    private readonly DateTimeOffset _fixedDateTimeProvider;
    private readonly DateOnly _expectedStartDate;
    private readonly DateOnly _expectedEndDate;

    public AdministrationReportServiceTests()
    {
        _bookRepositoryMock = new Mock<IBookRepository>();
        _borrowRepositoryMock = new Mock<IBorrowRepository>();
        _readerRepositoryMock = new Mock<IReaderRepository>();
        _administrationReportRepositoryMock = new Mock<IAdministrationReportRepository>();
        _reportGenerateServiceMock = new Mock<IReportGenerateService>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _emailServiceMock = new Mock<IEmailService>();
        _emailMessageTemplateServiceMock = new Mock<IEmailMessageTemplateService>();
        Mock<ILogger<AdministrationReportService>> loggerMock = new();

        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _fixedDateTimeProvider = new DateTimeOffset(2024, 01, 15, 10, 30, 00, TimeSpan.Zero);
        var timeProviderMock = new Mock<TimeProvider>();
        timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(_fixedDateTimeProvider);

        _reportsAdministrationBucketName = "test-reports-bucket";
        var minioOptions = new MinioOptions
        {
            ReportsAdministrationBucketName = _reportsAdministrationBucketName
        };
        var minioOptionsMonitor = new Mock<IOptionsMonitor<MinioOptions>>();
        minioOptionsMonitor.Setup(x => x.CurrentValue).Returns(minioOptions);

        _adminEmails = ["admin1@test.com", "admin2@test.com"];
        var emailOptions = new EmailOptions
        {
            AdminEmails = _adminEmails
        };
        var emailOptionsMonitor = new Mock<IOptionsMonitor<EmailOptions>>();
        emailOptionsMonitor.Setup(x => x.CurrentValue).Returns(emailOptions);

        _reportTemplateFileName = "report-template.html";
        _reportSubject = "Weekly Administration Report";

        var emailMessagesOptions = new EmailMessagesOptions
        {
            ReportForAdministration = new EmailInfo
            {
                TemplateFileName = _reportTemplateFileName,
                Subject = _reportSubject
            }
        };
        var emailMessagesOptionsMonitor = new Mock<IOptionsMonitor<EmailMessagesOptions>>();
        emailMessagesOptionsMonitor.Setup(x => x.CurrentValue).Returns(emailMessagesOptions);

        _reportName = "AdministrationReport";
        var backgroundReportsOptions = new BackgroundReportsOptions
        {
            ReportForAdministration = _reportName
        };
        var backgroundReportsOptionsMonitor = new Mock<IOptionsMonitor<BackgroundReportsOptions>>();
        backgroundReportsOptionsMonitor.Setup(x => x.CurrentValue).Returns(backgroundReportsOptions);

        _expectedStartDate = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.Date.AddDays(-7));
        _expectedEndDate = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.Date);

        _administrationReportService = new AdministrationReportService(
            _bookRepositoryMock.Object,
            _borrowRepositoryMock.Object,
            _readerRepositoryMock.Object,
            _emailServiceMock.Object,
            _fileStorageServiceMock.Object,
            emailOptionsMonitor.Object,
            emailMessagesOptionsMonitor.Object,
            backgroundReportsOptionsMonitor.Object,
            _reportGenerateServiceMock.Object,
            loggerMock.Object,
            minioOptionsMonitor.Object,
            timeProviderMock.Object,
            _administrationReportRepositoryMock.Object,
            _emailMessageTemplateServiceMock.Object);
    }
    
    [Fact]
    public async Task CreateReportForAdministration_Success_ShouldGenerateAndSaveAndSendReports()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedStartDateTime = _fixedDateTimeProvider.DateTime.Date.AddDays(-7);
        var expectedEndDateTime = _fixedDateTimeProvider.DateTime.Date;

        var addedBooksCount = _fixture.Create<int>();
        var newReadersCount = _fixture.Create<int>();
        var borrowStatistic = _fixture.Create<BorrowBookStatisticDto>();

        var expectedFileName = $"{_reportName}_{_fixedDateTimeProvider.DateTime:yyyy-MM-dd}.csv";
        var generatedAt = _fixedDateTimeProvider.DateTime;
        var reportContent = new MemoryStream();
        var reportUrl = _fixture.Create<string>();
        var htmlTemplate = _fixture.Create<string>();
        var contentType = "text/csv";
        var reportGenerateResult = _fixture
            .Build<ReportGenerateResult>()
            .With(r => r.GeneratedAt, generatedAt)
            .With(r => r.ContentType, contentType)
            .With(r => r.FileName, expectedFileName)
            .With(r => r.Content, reportContent)
            .Create();

        _bookRepositoryMock
            .Setup(x => x.GetAddedBooksCount(expectedStartDateTime, expectedEndDateTime, cancellationToken))
            .ReturnsAsync(addedBooksCount);

        _readerRepositoryMock
            .Setup(x => x.GetNewReadersCount(expectedStartDateTime, expectedEndDateTime, cancellationToken))
            .ReturnsAsync(newReadersCount);

        _borrowRepositoryMock
            .Setup(x => x.GetBorrowBookStatistic(_expectedStartDate, _expectedEndDate, cancellationToken))
            .ReturnsAsync(borrowStatistic);

        _reportGenerateServiceMock
            .Setup(x => x.GenerateReportForAdministration(_reportName,
                It.Is<BooksStatistic>(stats =>
                    stats.AddedBooksCount == addedBooksCount &&
                    stats.RegisterReadersCount == newReadersCount &&
                    stats.BorrowedCount == borrowStatistic.BorrowedCount &&
                    stats.ReturnedCount == borrowStatistic.ReturnedCount &&
                    stats.OverdueCount == borrowStatistic.OverdueCount &&
                    stats.PeriodFrom == _expectedStartDate &&
                    stats.PeriodTo == _expectedEndDate.AddDays(-1))))
            .Returns(reportGenerateResult);

        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_reportsAdministrationBucketName, expectedFileName, cancellationToken))
            .ReturnsAsync(reportUrl);

        _emailMessageTemplateServiceMock
            .Setup(x => x.GetEmailMessageHtmlBodyTemplateAsync(_reportTemplateFileName, cancellationToken))
            .ReturnsAsync(htmlTemplate);

        // Act
        await _administrationReportService.CreateReportForAdministration(cancellationToken);

        // Assert
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(_reportsAdministrationBucketName,
            expectedFileName, reportContent, "text/csv", cancellationToken), Times.Once);

        _fileStorageServiceMock.Verify(x => x.SetBucketFilesLifeTimeAsync(
            _reportsAdministrationBucketName,
            _fixedDateTimeProvider.DateTime.AddDays(90),
            $"{_fixedDateTimeProvider.DateTime:MM-dd}",
            cancellationToken), Times.Once);

        _administrationReportRepositoryMock.Verify(x => x.SaveReportAsync(
            It.Is<AdministrationReport>(r =>
                r.Name == expectedFileName &&
                r.Status == AdministrationReportStatus.Generated &&
                r.FilePath == reportUrl &&
                r.GeneratedAt == generatedAt &&
                r.CreatedAt == _fixedDateTimeProvider.DateTime),
            cancellationToken), Times.Once);

        _emailServiceMock.Verify(x => x.SendWeeklyReportToAdmin(
            _reportSubject,
            _adminEmails[0],
            It.Is<BooksStatistic>(stats =>
                stats.AddedBooksCount == addedBooksCount &&
                stats.RegisterReadersCount == newReadersCount &&
                stats.BorrowedCount == borrowStatistic.BorrowedCount &&
                stats.ReturnedCount == borrowStatistic.ReturnedCount &&
                stats.OverdueCount == borrowStatistic.OverdueCount &&
                stats.PeriodFrom == _expectedStartDate &&
                stats.PeriodTo == _expectedEndDate.AddDays(-1) &&
                stats.FileUrl == reportUrl),
            htmlTemplate,
            cancellationToken), Times.Once);

        _emailServiceMock.Verify(x => x.SendWeeklyReportToAdmin(
            _reportSubject,
            _adminEmails[1],
            It.Is<BooksStatistic>(stats =>
                stats.AddedBooksCount == addedBooksCount &&
                stats.RegisterReadersCount == newReadersCount &&
                stats.BorrowedCount == borrowStatistic.BorrowedCount &&
                stats.ReturnedCount == borrowStatistic.ReturnedCount &&
                stats.OverdueCount == borrowStatistic.OverdueCount &&
                stats.PeriodFrom == _expectedStartDate &&
                stats.PeriodTo == _expectedEndDate.AddDays(-1) &&
                stats.FileUrl == reportUrl),
            htmlTemplate,
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateReportForAdministration_WhenBorrowStatisticIsNull_ShouldSetZeroValues()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedStartDateTime = _fixedDateTimeProvider.DateTime.Date.AddDays(-7);
        var expectedEndDateTime = _fixedDateTimeProvider.DateTime.Date;

        var addedBooksCount = _fixture.Create<int>();
        var newReadersCount = _fixture.Create<int>();

        var expectedFileName = $"{_reportName}_{_fixedDateTimeProvider.DateTime:yyyy-MM-dd}.csv";
        var generatedAt = _fixedDateTimeProvider.DateTime;
        var reportContent = new MemoryStream();
        var reportUrl = _fixture.Create<string>();
        var htmlTemplate = _fixture.Create<string>();
        var contentType = "text/csv";
        var reportGenerateResult = _fixture
            .Build<ReportGenerateResult>()
            .With(r => r.GeneratedAt, generatedAt)
            .With(r => r.ContentType, contentType)
            .With(r => r.FileName, expectedFileName)
            .With(r => r.Content, reportContent)
            .Create();

        _bookRepositoryMock
            .Setup(x => x.GetAddedBooksCount(expectedStartDateTime, expectedEndDateTime, cancellationToken))
            .ReturnsAsync(addedBooksCount);

        _readerRepositoryMock
            .Setup(x => x.GetNewReadersCount(expectedStartDateTime, expectedEndDateTime, cancellationToken))
            .ReturnsAsync(newReadersCount);

        _borrowRepositoryMock
            .Setup(x => x.GetBorrowBookStatistic(_expectedStartDate, _expectedEndDate, cancellationToken))
            .ReturnsAsync((BorrowBookStatisticDto)null!);

        _reportGenerateServiceMock
            .Setup(x => x.GenerateReportForAdministration(_reportName,
                It.Is<BooksStatistic>(stats =>
                    stats.AddedBooksCount == addedBooksCount &&
                    stats.RegisterReadersCount == newReadersCount &&
                    stats.BorrowedCount == 0 &&
                    stats.ReturnedCount == 0 &&
                    stats.OverdueCount == 0 &&
                    stats.PeriodFrom == _expectedStartDate &&
                    stats.PeriodTo == _expectedEndDate.AddDays(-1))))
            .Returns(reportGenerateResult);

        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_reportsAdministrationBucketName, expectedFileName, cancellationToken))
            .ReturnsAsync(reportUrl);

        _emailMessageTemplateServiceMock
            .Setup(x => x.GetEmailMessageHtmlBodyTemplateAsync(_reportTemplateFileName, cancellationToken))
            .ReturnsAsync(htmlTemplate);

        // Act
        await _administrationReportService.CreateReportForAdministration(cancellationToken);

        // Assert
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(_reportsAdministrationBucketName,
            expectedFileName, reportContent, "text/csv", cancellationToken), Times.Once);

        _fileStorageServiceMock.Verify(x => x.SetBucketFilesLifeTimeAsync(
            _reportsAdministrationBucketName,
            _fixedDateTimeProvider.DateTime.AddDays(90),
            $"{_fixedDateTimeProvider.DateTime:MM-dd}",
            cancellationToken), Times.Once);

        _administrationReportRepositoryMock.Verify(x => x.SaveReportAsync(
            It.Is<AdministrationReport>(r =>
                r.Name == expectedFileName &&
                r.Status == AdministrationReportStatus.Generated &&
                r.FilePath == reportUrl &&
                r.GeneratedAt == generatedAt &&
                r.CreatedAt == _fixedDateTimeProvider.DateTime),
            cancellationToken), Times.Once);

        _emailServiceMock.Verify(x => x.SendWeeklyReportToAdmin(
            _reportSubject,
            _adminEmails[0],
            It.Is<BooksStatistic>(stats =>
                stats.AddedBooksCount == addedBooksCount &&
                stats.RegisterReadersCount == newReadersCount &&
                stats.BorrowedCount == 0 &&
                stats.ReturnedCount == 0 &&
                stats.OverdueCount == 0 &&
                stats.PeriodFrom == _expectedStartDate &&
                stats.PeriodTo == _expectedEndDate.AddDays(-1) &&
                stats.FileUrl == reportUrl),
            htmlTemplate,
            cancellationToken), Times.Once);

        _emailServiceMock.Verify(x => x.SendWeeklyReportToAdmin(
            _reportSubject,
            _adminEmails[1],
            It.Is<BooksStatistic>(stats =>
                stats.AddedBooksCount == addedBooksCount &&
                stats.RegisterReadersCount == newReadersCount &&
                stats.BorrowedCount == 0 &&
                stats.ReturnedCount == 0 &&
                stats.OverdueCount == 0 &&
                stats.PeriodFrom == _expectedStartDate &&
                stats.PeriodTo == _expectedEndDate.AddDays(-1) &&
                stats.FileUrl == reportUrl),
            htmlTemplate,
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateReportForAdministration_WhenFirstAdminEmailSendingFails_ShouldContinueSendingToSecondAdmin()
    {
                // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedStartDateTime = _fixedDateTimeProvider.DateTime.Date.AddDays(-7);
        var expectedEndDateTime = _fixedDateTimeProvider.DateTime.Date;

        var addedBooksCount = _fixture.Create<int>();
        var newReadersCount = _fixture.Create<int>();
        var borrowStatistic = _fixture.Create<BorrowBookStatisticDto>();

        var expectedFileName = $"{_reportName}_{_fixedDateTimeProvider.DateTime:yyyy-MM-dd}.csv";
        var generatedAt = _fixedDateTimeProvider.DateTime;
        var reportContent = new MemoryStream();
        var reportUrl = _fixture.Create<string>();
        var htmlTemplate = _fixture.Create<string>();
        var contentType = "text/csv";
        var reportGenerateResult = _fixture
            .Build<ReportGenerateResult>()
            .With(r => r.GeneratedAt, generatedAt)
            .With(r => r.ContentType, contentType)
            .With(r => r.FileName, expectedFileName)
            .With(r => r.Content, reportContent)
            .Create();

        _bookRepositoryMock
            .Setup(x => x.GetAddedBooksCount(expectedStartDateTime, expectedEndDateTime, cancellationToken))
            .ReturnsAsync(addedBooksCount);

        _readerRepositoryMock
            .Setup(x => x.GetNewReadersCount(expectedStartDateTime, expectedEndDateTime, cancellationToken))
            .ReturnsAsync(newReadersCount);

        _borrowRepositoryMock
            .Setup(x => x.GetBorrowBookStatistic(_expectedStartDate, _expectedEndDate, cancellationToken))
            .ReturnsAsync(borrowStatistic);

        _reportGenerateServiceMock
            .Setup(x => x.GenerateReportForAdministration(_reportName,
                It.Is<BooksStatistic>(stats =>
                    stats.AddedBooksCount == addedBooksCount &&
                    stats.RegisterReadersCount == newReadersCount &&
                    stats.BorrowedCount == borrowStatistic.BorrowedCount &&
                    stats.ReturnedCount == borrowStatistic.ReturnedCount &&
                    stats.OverdueCount == borrowStatistic.OverdueCount &&
                    stats.PeriodFrom == _expectedStartDate &&
                    stats.PeriodTo == _expectedEndDate.AddDays(-1))))
            .Returns(reportGenerateResult);

        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_reportsAdministrationBucketName, expectedFileName, cancellationToken))
            .ReturnsAsync(reportUrl);

        _emailMessageTemplateServiceMock
            .Setup(x => x.GetEmailMessageHtmlBodyTemplateAsync(_reportTemplateFileName, cancellationToken))
            .ReturnsAsync(htmlTemplate);

        _emailServiceMock.Setup(x => x.SendWeeklyReportToAdmin(_reportSubject,
            _adminEmails[0], It.IsAny<BooksStatistic>(),
            htmlTemplate, cancellationToken))
            .ThrowsAsync(new Exception());
        
        // Act
        await _administrationReportService.CreateReportForAdministration(cancellationToken);

        // Assert
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(_reportsAdministrationBucketName,
            expectedFileName, reportContent, "text/csv", cancellationToken), Times.Once);

        _fileStorageServiceMock.Verify(x => x.SetBucketFilesLifeTimeAsync(
            _reportsAdministrationBucketName,
            _fixedDateTimeProvider.DateTime.AddDays(90),
            $"{_fixedDateTimeProvider.DateTime:MM-dd}",
            cancellationToken), Times.Once);

        _administrationReportRepositoryMock.Verify(x => x.SaveReportAsync(
            It.Is<AdministrationReport>(r =>
                r.Name == expectedFileName &&
                r.Status == AdministrationReportStatus.Generated &&
                r.FilePath == reportUrl &&
                r.GeneratedAt == generatedAt &&
                r.CreatedAt == _fixedDateTimeProvider.DateTime),
            cancellationToken), Times.Once);

        _emailServiceMock.Verify(x => x.SendWeeklyReportToAdmin(
            _reportSubject,
            _adminEmails[1],
            It.Is<BooksStatistic>(stats =>
                stats.AddedBooksCount == addedBooksCount &&
                stats.RegisterReadersCount == newReadersCount &&
                stats.BorrowedCount == borrowStatistic.BorrowedCount &&
                stats.ReturnedCount == borrowStatistic.ReturnedCount &&
                stats.OverdueCount == borrowStatistic.OverdueCount &&
                stats.PeriodFrom == _expectedStartDate &&
                stats.PeriodTo == _expectedEndDate.AddDays(-1) &&
                stats.FileUrl == reportUrl),
            htmlTemplate,
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateReportForAdministration_WhenNoAdminEmails_ShouldNotSendAnyEmail()
    {
        // Arrange
        var previousAdminEmails = (_adminEmails[0], _adminEmails[1]);
        _adminEmails[0] += "1";
        _adminEmails[1] += "1";
        
        var cancellationToken = CancellationToken.None;
        var expectedStartDateTime = _fixedDateTimeProvider.DateTime.Date.AddDays(-7);
        var expectedEndDateTime = _fixedDateTimeProvider.DateTime.Date;

        var addedBooksCount = _fixture.Create<int>();
        var newReadersCount = _fixture.Create<int>();
        var borrowStatistic = _fixture.Create<BorrowBookStatisticDto>();

        var expectedFileName = $"{_reportName}_{_fixedDateTimeProvider.DateTime:yyyy-MM-dd}.csv";
        var generatedAt = _fixedDateTimeProvider.DateTime;
        var reportContent = new MemoryStream();
        var reportUrl = _fixture.Create<string>();
        var htmlTemplate = _fixture.Create<string>();
        var contentType = "text/csv";
        var reportGenerateResult = _fixture
            .Build<ReportGenerateResult>()
            .With(r => r.GeneratedAt, generatedAt)
            .With(r => r.ContentType, contentType)
            .With(r => r.FileName, expectedFileName)
            .With(r => r.Content, reportContent)
            .Create();

        _bookRepositoryMock
            .Setup(x => x.GetAddedBooksCount(expectedStartDateTime, expectedEndDateTime, cancellationToken))
            .ReturnsAsync(addedBooksCount);

        _readerRepositoryMock
            .Setup(x => x.GetNewReadersCount(expectedStartDateTime, expectedEndDateTime, cancellationToken))
            .ReturnsAsync(newReadersCount);

        _borrowRepositoryMock
            .Setup(x => x.GetBorrowBookStatistic(_expectedStartDate, _expectedEndDate, cancellationToken))
            .ReturnsAsync(borrowStatistic);

        _reportGenerateServiceMock
            .Setup(x => x.GenerateReportForAdministration(_reportName,
                It.Is<BooksStatistic>(stats =>
                    stats.AddedBooksCount == addedBooksCount &&
                    stats.RegisterReadersCount == newReadersCount &&
                    stats.BorrowedCount == borrowStatistic.BorrowedCount &&
                    stats.ReturnedCount == borrowStatistic.ReturnedCount &&
                    stats.OverdueCount == borrowStatistic.OverdueCount &&
                    stats.PeriodFrom == _expectedStartDate &&
                    stats.PeriodTo == _expectedEndDate.AddDays(-1))))
            .Returns(reportGenerateResult);

        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_reportsAdministrationBucketName, expectedFileName, cancellationToken))
            .ReturnsAsync(reportUrl);

        _emailMessageTemplateServiceMock
            .Setup(x => x.GetEmailMessageHtmlBodyTemplateAsync(_reportTemplateFileName, cancellationToken))
            .ReturnsAsync(htmlTemplate);

        // Act
        await _administrationReportService.CreateReportForAdministration(cancellationToken);

        // Assert
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(_reportsAdministrationBucketName,
            expectedFileName, reportContent, "text/csv", cancellationToken), Times.Once);

        _fileStorageServiceMock.Verify(x => x.SetBucketFilesLifeTimeAsync(
            _reportsAdministrationBucketName,
            _fixedDateTimeProvider.DateTime.AddDays(90),
            $"{_fixedDateTimeProvider.DateTime:MM-dd}",
            cancellationToken), Times.Once);

        _administrationReportRepositoryMock.Verify(x => x.SaveReportAsync(
            It.Is<AdministrationReport>(r =>
                r.Name == expectedFileName &&
                r.Status == AdministrationReportStatus.Generated &&
                r.FilePath == reportUrl &&
                r.GeneratedAt == generatedAt &&
                r.CreatedAt == _fixedDateTimeProvider.DateTime),
            cancellationToken), Times.Once);
        
        (_adminEmails[0], _adminEmails[1]) = previousAdminEmails;
        
        _emailServiceMock.Verify(x => x.SendWeeklyReportToAdmin(
            _reportSubject,
            _adminEmails[0],
            It.Is<BooksStatistic>(stats =>
                stats.AddedBooksCount == addedBooksCount &&
                stats.RegisterReadersCount == newReadersCount &&
                stats.BorrowedCount == borrowStatistic.BorrowedCount &&
                stats.ReturnedCount == borrowStatistic.ReturnedCount &&
                stats.OverdueCount == borrowStatistic.OverdueCount &&
                stats.PeriodFrom == _expectedStartDate &&
                stats.PeriodTo == _expectedEndDate.AddDays(-1) &&
                stats.FileUrl == reportUrl),
            htmlTemplate,
            cancellationToken), Times.Never);

        _emailServiceMock.Verify(x => x.SendWeeklyReportToAdmin(
            _reportSubject,
            _adminEmails[1],
            It.Is<BooksStatistic>(stats =>
                stats.AddedBooksCount == addedBooksCount &&
                stats.RegisterReadersCount == newReadersCount &&
                stats.BorrowedCount == borrowStatistic.BorrowedCount &&
                stats.ReturnedCount == borrowStatistic.ReturnedCount &&
                stats.OverdueCount == borrowStatistic.OverdueCount &&
                stats.PeriodFrom == _expectedStartDate &&
                stats.PeriodTo == _expectedEndDate.AddDays(-1) &&
                stats.FileUrl == reportUrl),
            htmlTemplate,
            cancellationToken), Times.Never);
    }
}