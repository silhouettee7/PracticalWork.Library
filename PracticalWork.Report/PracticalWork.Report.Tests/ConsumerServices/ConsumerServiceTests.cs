using AutoFixture;
using Domain.Abstractions.Services;
using Domain.Exceptions;
using Domain.Options;
using Microsoft.Extensions.Options;
using Moq;
using PracticalWork.Report.Abstractions.Services;
using PracticalWork.Report.Abstractions.Storage;
using PracticalWork.Report.Application.Services;
using PracticalWork.Report.Enums;
using PracticalWork.Report.Models;

namespace PracticalWork.Report.Tests.ConsumerServices;

public class ConsumerServiceTests
{
    private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock;
    private readonly Mock<IReportRepository> _reportRepositoryMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IActivityReportGenerateService> _activityReportGenerateServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Fixture _fixture = new();
    private readonly ConsumerService _consumerService;

    private readonly DateTimeOffset _fixedDateTimeOffset;
    private readonly string _reportsCacheVersionKey;
    private readonly string _reportsBucketName;
    
    public ConsumerServiceTests()
    {
        _activityLogRepositoryMock = new Mock<IActivityLogRepository>();
        _reportRepositoryMock = new Mock<IReportRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _activityReportGenerateServiceMock = new Mock<IActivityReportGenerateService>();
        _cacheServiceMock = new Mock<ICacheService>();
        
        Mock<TimeProvider> timeProviderMock = new();
        _fixedDateTimeOffset = new DateTimeOffset(2024, 01, 15, 10, 30, 00, TimeSpan.Zero);
        timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(_fixedDateTimeOffset);
        
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        _reportsCacheVersionKey = "reports:version:key";
        var redisOptions = new RedisOptions
        {
            Reports = new ReportsCacheConfig
            {
                VersionKey = _reportsCacheVersionKey
            }
        };
        var redisOptionsMonitor = new Mock<IOptionsMonitor<RedisOptions>>();
        redisOptionsMonitor.Setup(x => x.CurrentValue).Returns(redisOptions);
        
        _reportsBucketName = "test-reports-bucket";
        var minioOptions = new MinioOptions
        {
            ReportsBucketName = _reportsBucketName
        };
        var minioOptionsMonitor = new Mock<IOptionsMonitor<MinioOptions>>();
        minioOptionsMonitor.Setup(x => x.CurrentValue).Returns(minioOptions);
        
        _consumerService = new ConsumerService(
            _reportRepositoryMock.Object,
            _activityLogRepositoryMock.Object,
            _cacheServiceMock.Object,
            _fileStorageServiceMock.Object,
            _activityReportGenerateServiceMock.Object,
            minioOptionsMonitor.Object,
            redisOptionsMonitor.Object,
            timeProviderMock.Object);
    }
    
    #region WriteSystemActivityLogs Tests
    
    [Fact]
    public async Task WriteSystemActivityLogs_Success_ShouldAddLog()
    {
        // Arrange
        var activityLog = _fixture.Build<ActivityLog>()
            .Without(a => a.EventDate)
            .Without(a => a.Event)
            .Create();
        
        // Act
        await _consumerService.WriteSystemActivityLogs(activityLog);
        
        // Assert
        _activityLogRepositoryMock.Verify(x => x.AddLogAsync(activityLog), Times.Once);
    }
    
    [Fact]
    public async Task WriteSystemActivityLogs_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var activityLog = _fixture
            .Build<ActivityLog>()
            .Without(a => a.EventDate)
            .Without(a => a.Event)
            .Create();

        var dbException = new Exception("Database error");
        
        _activityLogRepositoryMock
            .Setup(x => x.AddLogAsync(activityLog))
            .ThrowsAsync(dbException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _consumerService.WriteSystemActivityLogs(activityLog));
        
        Assert.Equal("Database error", exception.Message);
    }
    
    #endregion
    
    #region GenerateReport Tests
    
    [Fact]
    public async Task GenerateReport_Success_ShouldGenerateAndUploadReport()
    {
        // Arrange
        var reportId = _fixture.Create<Guid>();
        var periodFrom = new DateOnly(2024, 01, 01);
        var periodTo = new DateOnly(2024, 12, 31);
        var eventTypes = _fixture
            .CreateMany<string>(2)
            .ToArray();
        
        var report = _fixture.Build<Models.Report>()
            .With(r => r.PeriodFrom, periodFrom)
            .With(r => r.PeriodTo, periodTo)
            .With(r => r.EventTypes, eventTypes)
            .With(r => r.Status, ReportStatus.InProgress)
            .Create();
        
        var logs = _fixture
            .Build<ActivityLog>()
            .Without(a => a.EventDate)
            .Without(a => a.Event)
            .CreateMany(5)
            .ToList();
        
        var reportResult = _fixture.Build<ReportGenerateResult>()
            .With(r => r.FileName, $"2024/01/{reportId}.csv")
            .With(r => r.Content, new MemoryStream())
            .With(r => r.ContentType, "text/csv")
            .Create();
        
        _reportRepositoryMock
            .Setup(x => x.GetReportById(reportId))
            .ReturnsAsync(report);
        
        _activityLogRepositoryMock
            .Setup(x => x.GetLogsAsync(periodFrom, periodTo, eventTypes))
            .ReturnsAsync(logs);
        
        _activityReportGenerateServiceMock
            .Setup(x => x.GenerateReport(reportId, logs))
            .Returns(reportResult);
        
        // Act
        await _consumerService.GenerateReport(reportId, periodFrom, periodTo, eventTypes);
        
        // Assert
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(
            _reportsBucketName,
            reportResult.FileName,
            reportResult.Content,
            reportResult.ContentType, 
            It.IsAny<CancellationToken>()), Times.Once);

        var reportName = reportResult.FileName.Split('/')[^1];
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m => 
                m.Status == ReportStatus.Generated && 
                m.EventTypes.SequenceEqual(eventTypes) && 
                m.Name == reportName && 
                m.PeriodFrom == periodFrom && 
                m.PeriodTo == periodTo && 
                m.GeneratedAt == _fixedDateTimeOffset.DateTime)), Times.Once);
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m => 
                m.Status == ReportStatus.Error)), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_reportsCacheVersionKey), Times.Once);
    }
    
    [Fact]
    public async Task GenerateReport_WhenNoFilters_ShouldPassNullToRepository()
    {
        // Arrange
        var reportId = _fixture.Create<Guid>();
        var report = _fixture.Build<Models.Report>()
            .Without(r => r.PeriodFrom)
            .Without(r => r.PeriodTo)
            .With(r => r.Status, ReportStatus.InProgress)
            .Create();
        
        var logs = _fixture
            .Build<ActivityLog>()
            .Without(a => a.EventDate)
            .Without(a => a.Event)
            .CreateMany(3)
            .ToList();
        
        var reportResult = _fixture.Build<ReportGenerateResult>()
            .With(r => r.FileName, $"2024/01/{reportId}.csv")
            .With(r => r.Content, new MemoryStream())
            .With(r => r.ContentType, "text/csv")
            .Create();
        
        _reportRepositoryMock
            .Setup(x => x.GetReportById(reportId))
            .ReturnsAsync(report);
        
        _activityLogRepositoryMock
            .Setup(x => x.GetLogsAsync(null, null, null))
            .ReturnsAsync(logs);
        
        _activityReportGenerateServiceMock
            .Setup(x => x.GenerateReport(reportId, logs))
            .Returns(reportResult);
        
        // Act
        await _consumerService.GenerateReport(reportId, null, null, null!);
        
        // Assert
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(
            _reportsBucketName,
            reportResult.FileName,
            reportResult.Content,
            reportResult.ContentType, 
            It.IsAny<CancellationToken>()), Times.Once);

        var reportName = reportResult.FileName.Split('/')[^1];
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m => 
                m.Status == ReportStatus.Generated && 
                m.Name == reportName && 
                m.GeneratedAt == _fixedDateTimeOffset.DateTime)), Times.Once);
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m => 
                m.Status == ReportStatus.Error)), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_reportsCacheVersionKey), Times.Once);
    }
    
    [Fact]
    public async Task GenerateReport_WhenEmptyEventTypes_ShouldPassEmptyArray()
    {
        // Arrange
        var reportId = _fixture.Create<Guid>();
        var periodFrom = new DateOnly(2024, 01, 01);
        var periodTo = new DateOnly(2024, 12, 31);
        var eventTypes = Array.Empty<string>();
        
        var report = _fixture.Build<Models.Report>()
            .With(r => r.PeriodFrom, periodFrom)
            .With(r => r.PeriodTo, periodTo)
            .With(r => r.EventTypes, eventTypes)
            .With(r => r.Status, ReportStatus.InProgress)
            .Create();
        
        var logs = _fixture
            .Build<ActivityLog>()
            .Without(a => a.EventDate)
            .Without(a => a.Event)
            .CreateMany(5).ToList();
        
        var reportResult = _fixture.Build<ReportGenerateResult>()
            .With(r => r.FileName, $"2024/01/{reportId}.csv")
            .With(r => r.Content, new MemoryStream())
            .With(r => r.ContentType, "text/csv")
            .Create();
        
        _reportRepositoryMock
            .Setup(x => x.GetReportById(reportId))
            .ReturnsAsync(report);
        
        _activityLogRepositoryMock
            .Setup(x => x.GetLogsAsync(periodFrom, periodTo, eventTypes))
            .ReturnsAsync(logs);
        
        _activityReportGenerateServiceMock
            .Setup(x => x.GenerateReport(reportId, logs))
            .Returns(reportResult);
        
        // Act
        await _consumerService.GenerateReport(reportId, periodFrom, periodTo, eventTypes);
        
        // Assert
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(
            _reportsBucketName,
            reportResult.FileName,
            reportResult.Content,
            reportResult.ContentType, 
            It.IsAny<CancellationToken>()), Times.Once);

        var reportName = reportResult.FileName.Split('/')[^1];
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m => 
                m.Status == ReportStatus.Generated && 
                m.EventTypes.SequenceEqual(eventTypes) && 
                m.Name == reportName && 
                m.PeriodFrom == periodFrom && 
                m.PeriodTo == periodTo && 
                m.GeneratedAt == _fixedDateTimeOffset.DateTime)), Times.Once);
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m => 
                m.Status == ReportStatus.Error)), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_reportsCacheVersionKey), Times.Once);
    }
    
    [Fact]
    public async Task GenerateReport_WhenReportGenerationFails_ShouldSetStatusToErrorAndInvalidateCache()
    {
        // Arrange
        var reportId = _fixture.Create<Guid>();
        var periodFrom = new DateOnly(2024, 01, 01);
        var periodTo = new DateOnly(2024, 12, 31);
        var eventTypes = _fixture.CreateMany<string>().ToArray();
        
        var report = _fixture.Build<Models.Report>()
            .With(r => r.PeriodFrom, periodFrom)
            .With(r => r.PeriodTo, periodTo)
            .With(r => r.EventTypes, eventTypes)
            .With(r => r.Status, ReportStatus.InProgress)
            .Create();
        
        var logs = _fixture
            .Build<ActivityLog>()
            .Without(a => a.EventDate)
            .Without(a => a.Event)
            .CreateMany(3).ToList();
        
        var generationException = new Exception("Report generation failed");
        
        _reportRepositoryMock
            .Setup(x => x.GetReportById(reportId))
            .ReturnsAsync(report);
        
        _activityLogRepositoryMock
            .Setup(x => x.GetLogsAsync(periodFrom, periodTo, eventTypes))
            .ReturnsAsync(logs);
        
        _activityReportGenerateServiceMock
            .Setup(x => x.GenerateReport(reportId, logs))
            .Throws(generationException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _consumerService.GenerateReport(reportId, periodFrom, periodTo, eventTypes));
        
        Assert.Equal("Report generation failed", exception.Message);
        
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m => 
                m.Status == ReportStatus.Error && 
                m.EventTypes.SequenceEqual(eventTypes) &&
                m.PeriodFrom == periodFrom && 
                m.PeriodTo == periodTo)), Times.Once);
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m => 
                m.Status == ReportStatus.Generated)), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_reportsCacheVersionKey), Times.Once);
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(
            It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<Stream>(), It.IsAny<string>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task GenerateReport_WhenUploadFails_ShouldSetStatusToErrorAndInvalidateCache()
    {
        // Arrange
        var reportId = _fixture.Create<Guid>();
        var periodFrom = new DateOnly(2024, 01, 01);
        var periodTo = new DateOnly(2024, 12, 31);
        var eventTypes = _fixture.CreateMany<string>().ToArray();
        
        var report = _fixture.Build<Models.Report>()
            .With(r => r.PeriodFrom, periodFrom)
            .With(r => r.PeriodTo, periodTo)
            .With(r => r.EventTypes, eventTypes)
            .With(r => r.Status, ReportStatus.InProgress)
            .Create();
        
        var logs = _fixture
            .Build<ActivityLog>()
            .Without(a => a.EventDate)
            .Without(a => a.Event)
            .CreateMany(5).ToList();
        
        var reportResult = _fixture.Build<ReportGenerateResult>()
            .With(r => r.FileName, $"2024/01/{reportId}.csv")
            .With(r => r.Content, new MemoryStream())
            .With(r => r.ContentType, "text/csv")
            .Create();
        
        _reportRepositoryMock
            .Setup(x => x.GetReportById(reportId))
            .ReturnsAsync(report);
        
        _activityLogRepositoryMock
            .Setup(x => x.GetLogsAsync(periodFrom, periodTo, eventTypes))
            .ReturnsAsync(logs);
        
        _activityReportGenerateServiceMock
            .Setup(x => x.GenerateReport(reportId, logs))
            .Returns(reportResult);
        
        var uploadException = new Exception("MinIO upload failed");
        _fileStorageServiceMock
            .Setup(x => x.UploadFileAsync(_reportsBucketName, reportResult.FileName, 
                reportResult.Content, reportResult.ContentType, It.IsAny<CancellationToken>()))
            .ThrowsAsync(uploadException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _consumerService.GenerateReport(reportId, periodFrom, periodTo, eventTypes));
        
        Assert.Equal("MinIO upload failed", exception.Message);
        
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m => 
                m.Status == ReportStatus.Error && 
                m.EventTypes.SequenceEqual(eventTypes) &&
                m.PeriodFrom == periodFrom && 
                m.PeriodTo == periodTo)), Times.Once);
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m => 
                m.Status == ReportStatus.Generated)), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_reportsCacheVersionKey), Times.Once);
    }
    
    [Fact]
    public async Task GenerateReport_WhenReportNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var reportId = _fixture.Create<Guid>();
        
        _reportRepositoryMock
            .Setup(x => x.GetReportById(reportId))
            .ThrowsAsync(new EntityNotFoundException($"Report with id {reportId} not found"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _consumerService.GenerateReport(reportId, null, null, null!));
        
        Assert.Contains($"Report with id {reportId} not found", exception.Message);
        
        _activityLogRepositoryMock.Verify(x => x.GetLogsAsync(
            It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<string[]>()), Times.Never);
        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), 
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.IsAny<Models.Report>()), Times.Never);
        _cacheServiceMock.Verify(x => x.InvalidateCache(_reportsCacheVersionKey), Times.Never);
    }
    #endregion
}