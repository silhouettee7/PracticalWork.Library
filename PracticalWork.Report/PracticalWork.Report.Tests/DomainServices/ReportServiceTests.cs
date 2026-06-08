using AutoFixture;
using Domain.Abstractions.MessageBroker;
using Domain.Abstractions.Services;
using Domain.Exceptions;
using Domain.Options;
using Microsoft.Extensions.Options;
using Moq;
using PracticalWork.Report.Abstractions.Storage;
using PracticalWork.Report.Application.Services;
using PracticalWork.Report.Enums;
using PracticalWork.Report.Events;

namespace PracticalWork.Report.Tests.DomainServices;

public class ReportServiceTests
{
    private readonly Mock<IReportRepository> _reportRepositoryMock;
    private readonly Mock<IRabbitMqPublisher> _publisherMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Fixture _fixture = new();
    private readonly ReportService _reportService;
    
    private readonly string _reportsExchangeName;
    private readonly string _reportsRoutingKey;
    private readonly string _reportsCacheVersionKey;
    private readonly string _reportsListCachePrefix;
    private readonly int _reportsListCacheTtlInMinutes;
    private readonly string _reportsBucketName;
    private readonly DateTimeOffset _fixedDateTimeProvider;
    
    public ReportServiceTests()
    {
        _reportRepositoryMock = new Mock<IReportRepository>();
        _publisherMock = new Mock<IRabbitMqPublisher>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _cacheServiceMock = new Mock<ICacheService>();
        
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        _fixedDateTimeProvider = new DateTimeOffset(2024, 01, 15, 10, 30, 00, TimeSpan.Zero);
        var timeProviderMock = new Mock<TimeProvider>();
        timeProviderMock
            .Setup(t => t.GetUtcNow())
            .Returns(_fixedDateTimeProvider);
        
        _reportsExchangeName = "reports.exchange";
        _reportsRoutingKey = "report.create";
        var rabbitOptions = new RabbitOptions
        {
            Reports = new()
            {
                Exchange = _reportsExchangeName,
                RoutingKey = _reportsRoutingKey
            }
        };
        var rabbitOptionsMonitor = new Mock<IOptionsMonitor<RabbitOptions>>();
        rabbitOptionsMonitor.Setup(x => x.CurrentValue).Returns(rabbitOptions);
        
        _reportsCacheVersionKey = "reports:version:key";
        _reportsListCachePrefix = "reports:list";
        _reportsListCacheTtlInMinutes = 10;
        var redisOptions = new RedisOptions
        {
            Reports = new ReportsCacheConfig
            {
                VersionKey = _reportsCacheVersionKey,
                ReportsList = new CacheEntryConfig
                {
                    Prefix = _reportsListCachePrefix,
                    TtlInMinutes = _reportsListCacheTtlInMinutes
                }
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
        
        _reportService = new ReportService(
            _reportRepositoryMock.Object,
            _publisherMock.Object,
            _cacheServiceMock.Object,
            _fileStorageServiceMock.Object,
            minioOptionsMonitor.Object,
            redisOptionsMonitor.Object,
            rabbitOptionsMonitor.Object,
            timeProviderMock.Object);
    }
    
    #region CreateReport Tests
    
    [Fact]
    public async Task CreateReport_Success_ShouldCreateReportAndPublishEvent()
    {
        // Arrange
        var eventDateFrom = new DateOnly(2024, 01, 01);
        var eventDateTo = new DateOnly(2024, 12, 31);
        var eventTypes = _fixture.CreateMany<string>().ToArray();
        var expectedId = _fixture.Create<Guid>();
        
        _reportRepositoryMock
            .Setup(x => x.CreateReport(
                It.Is<Models.Report>(r => 
                    r.PeriodFrom == eventDateFrom &&
                    r.PeriodTo == eventDateTo &&
                    r.EventTypes.SequenceEqual(eventTypes) &&
                    r.Status == ReportStatus.InProgress)))
            .ReturnsAsync(expectedId);
        
        // Act
        var result = await _reportService.CreateReport(eventDateFrom, eventDateTo, eventTypes);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventDateFrom, result.PeriodFrom);
        Assert.Equal(eventDateTo, result.PeriodTo);
        Assert.Equal(eventTypes, result.EventTypes);
        Assert.Equal(ReportStatus.InProgress, result.Status);
        
        _publisherMock.Verify(x => x.PublishAsync(
            _reportsExchangeName,
            _reportsRoutingKey,
            It.Is<ReportCreateEvent>(e =>
                e.Id == expectedId &&
                e.PeriodFrom == eventDateFrom &&
                e.PeriodTo == eventDateTo &&
                e.EventTypes.SequenceEqual(eventTypes) &&
                e.Status == ReportStatus.InProgress),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _cacheServiceMock.Verify(x => x.InvalidateCache(_reportsCacheVersionKey), Times.Once);
    }
    
    [Fact]
    public async Task CreateReport_WithNullParameters_ShouldCreateReportWithNulls()
    {
        // Arrange
        var expectedId = _fixture.Create<Guid>();
        
        _reportRepositoryMock
            .Setup(x => x.CreateReport(
                It.Is<Models.Report>(r => 
                    r.PeriodFrom == null &&
                    r.PeriodTo == null &&
                    r.EventTypes == null &&
                    r.Status == ReportStatus.InProgress)))
            .ReturnsAsync(expectedId);
        // Act
        var result = await _reportService.CreateReport(null, null, null!);
        
        // Assert
        Assert.NotNull(result);
        Assert.Null(result.PeriodFrom);
        Assert.Null(result.PeriodTo);
        Assert.Null(result.EventTypes);
        
        _publisherMock.Verify(x => x.PublishAsync(
            _reportsExchangeName,
            _reportsRoutingKey,
            It.Is<ReportCreateEvent>(e =>
                e.Id == expectedId &&
                e.PeriodFrom == null &&
                e.PeriodTo == null &&
                e.EventTypes == null),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _cacheServiceMock.Verify(x => x.InvalidateCache(_reportsCacheVersionKey), Times.Once);
    }
    
    [Fact]
    public async Task CreateReport_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var eventDateFrom = new DateOnly(2024, 01, 01);
        var eventDateTo = new DateOnly(2024, 12, 31);
        var eventTypes = _fixture.CreateMany<string>().ToArray();
        var dbException = new Exception("Database error");
        
        _reportRepositoryMock
            .Setup(x => x.CreateReport(It.IsAny<Models.Report>()))
            .ThrowsAsync(dbException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _reportService.CreateReport(eventDateFrom, eventDateTo, eventTypes));
        
        Assert.Equal("Database error", exception.Message);
        
        _publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<ReportCreateEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        
        _cacheServiceMock.Verify(x => x.InvalidateCache(It.IsAny<string>()), Times.Never);
    }
    
    #endregion
    
    #region GetListOfReadyReports Tests
    
    [Fact]
    public async Task GetListOfReadyReports_CacheHit_ShouldReturnCachedResult()
    {
        // Arrange
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        var cachedReports = _fixture
            .Build<Models.Report>()
            .Without(r => r.PeriodFrom)
            .Without(r => r.PeriodTo)
            .CreateMany(5)
            .ToList();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_reportsCacheVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_reportsListCachePrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<Models.Report>>(cacheKey))
            .ReturnsAsync(cachedReports);
        
        // Act
        var result = await _reportService.GetListOfReadyReports();
        
        // Assert
        Assert.Equivalent(cachedReports, result);
        _reportRepositoryMock.Verify(x => x.GetReadyReports(), Times.Never);
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>()), Times.Never);
    }
    
    [Fact]
    public async Task GetListOfReadyReports_CacheMiss_ShouldQueryDatabaseAndCacheResult()
    {
        // Arrange
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        var reports = _fixture
            .Build<Models.Report>()
            .Without(r => r.PeriodFrom)
            .Without(r => r.PeriodTo)
            .CreateMany(5)
            .ToList();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_reportsCacheVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_reportsListCachePrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<Models.Report>>(cacheKey))
            .ReturnsAsync((IReadOnlyList<Models.Report>)null!);
        
        _reportRepositoryMock
            .Setup(x => x.GetReadyReports())
            .ReturnsAsync(reports);
        
        // Act
        var result = await _reportService.GetListOfReadyReports();
        
        // Assert
        Assert.Equivalent(reports, result);
        
        _cacheServiceMock.Verify(x => x.SetAsync(
            cacheKey,
            reports as IReadOnlyList<Models.Report>,
            TimeSpan.FromMinutes(_reportsListCacheTtlInMinutes)), Times.Once);
    }
    
    [Fact]
    public async Task GetListOfReadyReports_WhenEmptyList_ShouldCacheEmptyList()
    {
        // Arrange
        var cacheVersion = _fixture.Create<long>();
        var cacheKey = _fixture.Create<string>();
        var emptyReports = new List<Models.Report>();
        
        _cacheServiceMock
            .Setup(x => x.GetCurrentCacheVersion(_reportsCacheVersionKey))
            .ReturnsAsync(cacheVersion);
        
        _cacheServiceMock
            .Setup(x => x.GenerateCacheKey(_reportsListCachePrefix, cacheVersion, null))
            .Returns(cacheKey);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<Models.Report>>(cacheKey))
            .ReturnsAsync((IReadOnlyList<Models.Report>)null!);
        
        _reportRepositoryMock
            .Setup(x => x.GetReadyReports())
            .ReturnsAsync(emptyReports);
        
        // Act
        var result = await _reportService.GetListOfReadyReports();
        
        // Assert
        Assert.Empty(result);
        
        _cacheServiceMock.Verify(x => x.SetAsync(
            cacheKey,
            emptyReports as IReadOnlyList<Models.Report>,
            TimeSpan.FromMinutes(_reportsListCacheTtlInMinutes)), Times.Once);
    }
    
    #endregion
    
    #region GetReportUrl Tests
    
    [Fact]
    public async Task GetReportUrl_Success_ShouldReturnUrlAndUpdateReport()
    {
        // Arrange
        var reportName = _fixture.Create<string>();
        var reportId = _fixture.Create<Guid>();
        var generatedAt = new DateTime(2024, 01, 15, 10, 30, 00);
        var expectedFilePath = $"{generatedAt.Year}/{generatedAt.Month}/{reportName}";
        var expectedUrl = _fixture.Create<string>();
        
        var report = _fixture.Build<Models.Report>()
            .Without(r => r.PeriodFrom)
            .Without(r => r.PeriodTo)
            .With(r => r.GeneratedAt, generatedAt)
            .With(r => r.FilePath, (string?)null)
            .Create();
        
        _reportRepositoryMock
            .Setup(x => x.GetReportByName(reportName))
            .ReturnsAsync((reportId, report));
        
        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_reportsBucketName, expectedFilePath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);
        
        // Act
        var result = await _reportService.GetReportUrl(reportName);
        
        // Assert
        Assert.Equal(expectedUrl, result);
        
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m =>
                m.FilePath == expectedUrl && 
                m.GeneratedAt == report.GeneratedAt)), Times.Once);
    }
    
    [Fact]
    public async Task GetReportUrl_WhenGeneratedAtIsNull_ShouldUseCurrentDate()
    {
        // Arrange
        var reportName = _fixture.Create<string>();
        var reportId = _fixture.Create<Guid>();
        var generatedAt = _fixedDateTimeProvider.DateTime;
        var expectedFilePath = $"{generatedAt.Year}/{generatedAt.Month}/{reportName}";
        var expectedUrl = _fixture.Create<string>();
        
        var report = _fixture.Build<Models.Report>()
            .Without(r => r.PeriodFrom)
            .Without(r => r.PeriodTo)
            .With(r => r.GeneratedAt, (DateTime?)null)
            .With(r => r.FilePath, (string?)null)
            .Create();
        
        _reportRepositoryMock
            .Setup(x => x.GetReportByName(reportName))
            .ReturnsAsync((reportId, report));
        
        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(_reportsBucketName, expectedFilePath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);
        
        // Act
        var result = await _reportService.GetReportUrl(reportName);
        
        // Assert
        Assert.Equal(expectedUrl, result);
        
        _reportRepositoryMock.Verify(x => x.UpdateReport(reportId, 
            It.Is<Models.Report>(m =>
                m.FilePath == expectedUrl && 
                m.GeneratedAt == report.GeneratedAt)), Times.Once);
    }
    
    [Fact]
    public async Task GetReportUrl_WhenFileStorageFails_ShouldThrowException()
    {
        // Arrange
        var reportName = _fixture.Create<string>();
        var reportId = _fixture.Create<Guid>();
        var generatedAt = new DateTime(2024, 01, 15, 10, 30, 00);
        var expectedFilePath = $"{generatedAt.Year}/{generatedAt.Month}/{reportName}";
        
        var report = _fixture.Build<Models.Report>()
            .Without(r => r.PeriodFrom)
            .Without(r => r.PeriodTo)
            .With(r => r.GeneratedAt, generatedAt)
            .Create();
        
        _reportRepositoryMock
            .Setup(x => x.GetReportByName(reportName))
            .ReturnsAsync((reportId, report));
        
        var storageException = new Exception("MinIO connection failed");
        _fileStorageServiceMock
            .Setup(x => x.GetFileLinkAsync(
                _reportsBucketName, expectedFilePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(storageException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _reportService.GetReportUrl(reportName));
        
        Assert.Equal("MinIO connection failed", exception.Message);
        
        _reportRepositoryMock.Verify(x => x.UpdateReport(
            It.IsAny<Guid>(), It.IsAny<Models.Report>()), Times.Never);
    }
    
    [Fact]
    public async Task GetReportUrl_WhenReportNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var reportName = _fixture.Create<string>();
        
        _reportRepositoryMock
            .Setup(x => x.GetReportByName(reportName))
            .ThrowsAsync(new EntityNotFoundException($"Report with name {reportName} not found"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _reportService.GetReportUrl(reportName));
        
        Assert.Contains(reportName, exception.Message);
        
        _fileStorageServiceMock.Verify(x => x.GetFileLinkAsync(
            It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<CancellationToken>()), Times.Never);
        
        _reportRepositoryMock.Verify(x => x.UpdateReport(
            It.IsAny<Guid>(), It.IsAny<Models.Report>()), Times.Never);
    }
    
    #endregion
}