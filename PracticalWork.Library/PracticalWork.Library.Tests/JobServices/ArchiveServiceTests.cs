using AutoFixture;
using Domain.Abstractions.Services;
using Domain.Models;
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

public class ArchiveServiceTests
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IBookService> _bookServiceMock;
    private readonly Mock<IReportGenerateService> _reportGenerateServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IAdministrationReportRepository> _administrationReportRepositoryMock;
    private readonly Mock<ILogger<ArchiveService>> _loggerMock;
    private readonly Fixture _fixture = new();
    private readonly ArchiveService _archiveService;
    
    private readonly string _archiveBooksBucketName;
    private readonly string _reportAboutArchive;
    private readonly DateTimeOffset _fixedDateTimeProvider;
    private readonly DateOnly _expectedDateThreeYearsAgo;
    
    public ArchiveServiceTests()
    {
        _bookRepositoryMock = new Mock<IBookRepository>();
        _bookServiceMock = new Mock<IBookService>();
        _reportGenerateServiceMock = new Mock<IReportGenerateService>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _administrationReportRepositoryMock = new Mock<IAdministrationReportRepository>();
        _loggerMock = new Mock<ILogger<ArchiveService>>();
        
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        _fixedDateTimeProvider = new DateTimeOffset(2024, 01, 15, 10, 30, 00, TimeSpan.Zero);
        var timeProviderMock = new Mock<TimeProvider>();
        timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(_fixedDateTimeProvider);

        _archiveBooksBucketName = "test-archive-bucket";
        var minioOptions = new MinioOptions
        {
            ArchiveBooksBucketName = _archiveBooksBucketName
        };
        var minioOptionsMonitor = new Mock<IOptionsMonitor<MinioOptions>>();
        minioOptionsMonitor.Setup(x => x.CurrentValue).Returns(minioOptions);
        
        _reportAboutArchive = "ArchiveReport";
        var backgroundReportsOptions = new BackgroundReportsOptions
        {
            ReportAboutArchive = _reportAboutArchive
        };
        var backgroundReportsOptionsMonitor = new Mock<IOptionsMonitor<BackgroundReportsOptions>>();
        backgroundReportsOptionsMonitor.Setup(x => x.CurrentValue).Returns(backgroundReportsOptions);
        
        _expectedDateThreeYearsAgo = DateOnly.FromDateTime(_fixedDateTimeProvider.DateTime.AddYears(-3));
        
        _archiveService = new ArchiveService(
            _bookRepositoryMock.Object,
            _bookServiceMock.Object,
            _reportGenerateServiceMock.Object,
            _loggerMock.Object,
            _fileStorageServiceMock.Object,
            minioOptionsMonitor.Object,
            _administrationReportRepositoryMock.Object,
            timeProviderMock.Object,
            backgroundReportsOptionsMonitor.Object);
    }
    
    [Fact]
    public async Task ArchiveOldBooksAsync_Success_ShouldArchiveAllBooksAndGenerateReport()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var oldBooks = _fixture.CreateMany<AvailableOldBookDto>(5).ToList();
        var expectedFileName = $"{_fixedDateTimeProvider.Year}/{_reportAboutArchive}_{_fixedDateTimeProvider.Month}.csv";
        var reportContent = new MemoryStream();
        var reportUrl = _fixture.Create<string>();
        
        _bookRepositoryMock
            .Setup(x => x.GetAvailableOldBooksPage(
                _expectedDateThreeYearsAgo,
                It.Is<CursorPaginationRequest>(r => r.PageSize == 100 && r.Forward == true),
                cancellationToken))
            .ReturnsAsync(oldBooks);
        
        _reportGenerateServiceMock
            .Setup(x => x.GenerateArchiveReport(_reportAboutArchive, 
                It.Is<ArchiveLog>(log => 
                    log.TotalCount == oldBooks.Count &&
                    log.SuccessCount == oldBooks.Count &&
                    log.WrongCount == 0)))
            .Returns(new ReportGenerateResult
            {
                FileName = expectedFileName,
                Content = reportContent,
                ContentType = "text/csv",
                GeneratedAt = _fixedDateTimeProvider.DateTime
            });
        
        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_archiveBooksBucketName, expectedFileName, cancellationToken))
            .ReturnsAsync(reportUrl);
        
        // Act
        await _archiveService.ArchiveOldBooksAsync(cancellationToken);
        
        // Assert
        foreach (var book in oldBooks)
        {
            _bookServiceMock.Verify(x => x.ArchiveBook(book.Id, cancellationToken), Times.Once);
        }
        
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(
            _archiveBooksBucketName, expectedFileName, reportContent, "text/csv", cancellationToken), Times.Once);
        
        _administrationReportRepositoryMock.Verify(x => x.SaveReportAsync(
            It.Is<AdministrationReport>(r =>
                r.Name == expectedFileName &&
                r.Status == AdministrationReportStatus.Generated &&
                r.FilePath == reportUrl &&
                r.CreatedAt == _fixedDateTimeProvider.DateTime &&
                r.GeneratedAt == _fixedDateTimeProvider.DateTime),
            cancellationToken), Times.Once);
    }
    
    [Fact]
    public async Task ArchiveOldBooksAsync_NoOldBooks_ShouldNotArchiveAnythingAndGenerateEmptyReport()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var emptyBooksList = new List<AvailableOldBookDto>();
        var expectedFileName = $"{_fixedDateTimeProvider.Year}/{_reportAboutArchive}_{_fixedDateTimeProvider.Month}.csv";
        var reportContent = new MemoryStream();
        var reportUrl = _fixture.Create<string>();
        
        _bookRepositoryMock
            .Setup(x => x.GetAvailableOldBooksPage(
                _expectedDateThreeYearsAgo,
                It.Is<CursorPaginationRequest>(r => r.PageSize == 100 && r.Forward == true),
                cancellationToken))
            .ReturnsAsync(emptyBooksList);
        
        _reportGenerateServiceMock
            .Setup(x => x.GenerateArchiveReport(_reportAboutArchive, 
                It.Is<ArchiveLog>(log => 
                    log.TotalCount == 0 &&
                    log.SuccessCount == 0 &&
                    log.WrongCount == 0)))
            .Returns(new ReportGenerateResult
            {
                FileName = expectedFileName,
                Content = reportContent,
                ContentType = "text/csv",
                GeneratedAt = _fixedDateTimeProvider.DateTime
            });
        
        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_archiveBooksBucketName, expectedFileName, cancellationToken))
            .ReturnsAsync(reportUrl);
        
        // Act
        await _archiveService.ArchiveOldBooksAsync(cancellationToken);
        
        // Assert
        _bookServiceMock.Verify(x => x.ArchiveBook(It.IsAny<Guid>(), cancellationToken), Times.Never);
        
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(
            _archiveBooksBucketName, expectedFileName, reportContent, "text/csv", cancellationToken), Times.Once);
        
        _administrationReportRepositoryMock.Verify(x => x.SaveReportAsync(
            It.Is<AdministrationReport>(r =>
                r.Name == expectedFileName &&
                r.Status == AdministrationReportStatus.Generated &&
                r.FilePath == reportUrl &&
                r.CreatedAt == _fixedDateTimeProvider.DateTime &&
                r.GeneratedAt == _fixedDateTimeProvider.DateTime), cancellationToken), 
            Times.Once);
    }
    
    [Fact]
    public async Task ArchiveOldBooksAsync_SomeBooksFailToArchive_ShouldContinue()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var oldBooks = _fixture.CreateMany<AvailableOldBookDto>(5).ToList();
        var expectedFileName = $"{_fixedDateTimeProvider.Year}/{_reportAboutArchive}_{_fixedDateTimeProvider.Month}.csv";
        var reportContent = new MemoryStream();
        var reportUrl = _fixture.Create<string>();
        
        var exceptionMessage = "Book is currently borrowed";
        var exception = new Exception(exceptionMessage);
        
        _bookRepositoryMock
            .Setup(x => x.GetAvailableOldBooksPage(
                _expectedDateThreeYearsAgo,
                It.Is<CursorPaginationRequest>(r => r.PageSize == 100 && r.Forward == true),
                cancellationToken))
            .ReturnsAsync(oldBooks);

        _bookServiceMock
            .Setup(x => x.ArchiveBook(oldBooks[0].Id, cancellationToken))
            .ReturnsAsync(new BookArchive());
        
        _bookServiceMock
            .Setup(x => x.ArchiveBook(oldBooks[1].Id, cancellationToken))
            .ThrowsAsync(exception);
        
        _bookServiceMock
            .Setup(x => x.ArchiveBook(oldBooks[2].Id, cancellationToken))
            .ReturnsAsync(new BookArchive());
        
        _bookServiceMock
            .Setup(x => x.ArchiveBook(oldBooks[3].Id, cancellationToken))
            .ThrowsAsync(new Exception("Another error"));
        
        _bookServiceMock
            .Setup(x => x.ArchiveBook(oldBooks[4].Id, cancellationToken))
            .ReturnsAsync(new BookArchive());
        
        _reportGenerateServiceMock
            .Setup(x => x.GenerateArchiveReport(_reportAboutArchive, 
                    It.Is<ArchiveLog>(log => 
                        log.TotalCount == oldBooks.Count &&
                        log.SuccessCount == 3 &&
                        log.WrongCount == 2 &&
                        log.WrongReasons.Contains(exceptionMessage) &&
                        log.WrongReasons.Contains("Another error"))))
            .Returns(new ReportGenerateResult
            {
                FileName = expectedFileName,
                Content = reportContent,
                ContentType = "text/csv",
                GeneratedAt = _fixedDateTimeProvider.DateTime
            });
        
        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_archiveBooksBucketName, expectedFileName, cancellationToken))
            .ReturnsAsync(reportUrl);
        
        // Act
        await _archiveService.ArchiveOldBooksAsync(cancellationToken);
        
        // Assert
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(
            _archiveBooksBucketName, expectedFileName, reportContent, "text/csv", cancellationToken), Times.Once);
        
        _administrationReportRepositoryMock.Verify(x => x.SaveReportAsync(
                It.Is<AdministrationReport>(r =>
                    r.Name == expectedFileName &&
                    r.Status == AdministrationReportStatus.Generated &&
                    r.FilePath == reportUrl &&
                    r.CreatedAt == _fixedDateTimeProvider.DateTime &&
                    r.GeneratedAt == _fixedDateTimeProvider.DateTime), cancellationToken), 
            Times.Once);
    }
    
    [Fact]
    public async Task ArchiveOldBooksAsync_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var dbException = new Exception("Database connection error");
        
        _bookRepositoryMock
            .Setup(x => x.GetAvailableOldBooksPage(
                _expectedDateThreeYearsAgo,
                It.Is<CursorPaginationRequest>(r => r.PageSize == 100 && r.Forward == true),
                cancellationToken))
            .ThrowsAsync(dbException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _archiveService.ArchiveOldBooksAsync(cancellationToken));
        
        Assert.Equal("Database connection error", exception.Message);
        
        _bookServiceMock.Verify(x => x.ArchiveBook(It.IsAny<Guid>(), cancellationToken), Times.Never);
        _reportGenerateServiceMock.Verify(x => x.GenerateArchiveReport(
            It.IsAny<string>(), It.IsAny<ArchiveLog>()), Times.Never);
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), 
            It.IsAny<string>(), cancellationToken), Times.Never);
        _administrationReportRepositoryMock.Verify(x => x.SaveReportAsync(
                It.IsAny<AdministrationReport>(), cancellationToken), 
            Times.Never);
    }
    
    [Fact]
    public async Task ArchiveOldBooksAsync_WhenSaveReportFails_ShouldPropagateException()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var oldBooks = _fixture.CreateMany<AvailableOldBookDto>(2).ToList();
        var expectedFileName = _fixture.Create<string>();
        var reportContent = new MemoryStream();
        
        _bookRepositoryMock
            .Setup(x => x.GetAvailableOldBooksPage(
                _expectedDateThreeYearsAgo,
                It.IsAny<CursorPaginationRequest>(),
                cancellationToken))
            .ReturnsAsync(oldBooks);
        
        _reportGenerateServiceMock
            .Setup(x => x.GenerateArchiveReport(_reportAboutArchive, It.IsAny<ArchiveLog>()))
            .Returns(new ReportGenerateResult
            {
                FileName = expectedFileName,
                Content = reportContent,
                ContentType = "text/csv",
                GeneratedAt = _fixedDateTimeProvider.DateTime
            });
        
        var uploadException = new Exception("MinIO upload failed");
        _fileStorageServiceMock
            .Setup(x => x.UploadFileAsync(_archiveBooksBucketName, expectedFileName, 
                reportContent, "text/csv", cancellationToken))
            .ThrowsAsync(uploadException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _archiveService.ArchiveOldBooksAsync(cancellationToken));
        
        Assert.Equal("MinIO upload failed", exception.Message);
        
        _administrationReportRepositoryMock.Verify(x => x.SaveReportAsync(
            It.IsAny<AdministrationReport>(), cancellationToken), Times.Never);
    }
    
    [Fact]
    public async Task ArchiveOldBooksAsync_ShouldHandleDuplicateErrorMessagesInLog()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var oldBooks = _fixture.CreateMany<AvailableOldBookDto>(5).ToList();
        var expectedFileName = $"{_fixedDateTimeProvider.Year}/{_reportAboutArchive}_{_fixedDateTimeProvider.Month}.csv";
        var reportContent = new MemoryStream();
        var reportUrl = _fixture.Create<string>();
        var sameErrorMessage = "Book is currently borrowed";
        
        _bookRepositoryMock
            .Setup(x => x.GetAvailableOldBooksPage(
                _expectedDateThreeYearsAgo,
                It.IsAny<CursorPaginationRequest>(),
                cancellationToken))
            .ReturnsAsync(oldBooks);
        
        foreach (var book in oldBooks)
        {
            _bookServiceMock
                .Setup(x => x.ArchiveBook(book.Id, cancellationToken))
                .ThrowsAsync(new Exception(sameErrorMessage));
        }
        
        _reportGenerateServiceMock
            .Setup(x => x.GenerateArchiveReport(
                _reportAboutArchive,
                It.Is<ArchiveLog>(log =>
                    log.WrongCount == oldBooks.Count &&
                    !log.WrongReasons.Contains(";\n"))))
            .Returns(new ReportGenerateResult
            {
                FileName = expectedFileName,
                Content = reportContent,
                ContentType = "text/csv",
                GeneratedAt = _fixedDateTimeProvider.DateTime
            });
        
        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_archiveBooksBucketName, expectedFileName, cancellationToken))
            .ReturnsAsync(reportUrl);
        
        // Act
        await _archiveService.ArchiveOldBooksAsync(cancellationToken);
        
        // Assert
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(_archiveBooksBucketName,
                expectedFileName, reportContent, "text/csv", cancellationToken),
            Times.Once);
        
        _administrationReportRepositoryMock.Verify(x => x.SaveReportAsync(
            It.Is<AdministrationReport>(r =>
                r.Name == expectedFileName &&   
                r.Status == AdministrationReportStatus.Generated &&
                r.FilePath == reportUrl &&
                r.CreatedAt == _fixedDateTimeProvider.DateTime &&
                r.GeneratedAt == _fixedDateTimeProvider.DateTime),
            cancellationToken), Times.Once);
    }
}