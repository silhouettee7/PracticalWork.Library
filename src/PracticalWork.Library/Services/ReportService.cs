using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Events;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Services;

public class ReportService: IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly IRabbitMqPublisher _publisher;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICacheService _cacheService;
    private readonly string _reportsExchangeName;
    private readonly string _reportsRoutingKey;
    private readonly string _reportsCacheVersionKey;
    private readonly string _reportsListCachePrefix;
    private readonly double _reportsListCacheTtlInMinutes;
    private readonly string _reportsBucketName;

    public ReportService(
        IReportRepository reportRepository,
        IRabbitMqPublisher publisher,
        ICacheService cacheService,
        IFileStorageService fileStorageService,
        IOptionsMonitor<MinioOptions> minioOptions,
        IOptionsMonitor<RedisOptions> redisOptions,
        IOptionsMonitor<RabbitOptions> rabbitOptions)
    {
        _reportRepository = reportRepository;
        _cacheService = cacheService;
        _fileStorageService = fileStorageService;
        _publisher = publisher;
        var minioOpt = minioOptions.CurrentValue;
        var redisOpt = redisOptions.CurrentValue;
        var rabbitOpt = rabbitOptions.CurrentValue;
        
        _reportsExchangeName = rabbitOpt.Reports.Exchange;
        _reportsRoutingKey = rabbitOpt.Reports.RoutingKey;
        _reportsCacheVersionKey = redisOpt.Reports.VersionKey;
        _reportsListCachePrefix = redisOpt.Reports.ReportsList.Prefix;
        _reportsListCacheTtlInMinutes = redisOpt.Reports.ReportsList.TtlInMinutes;
        _reportsBucketName = minioOpt.ReportsBucketName;
    }

    public async Task<Report> CreateReport(DateOnly? eventDateFrom, DateOnly? eventDateTo, string[] eventTypes)
    {
        var report = new Report
        {
            PeriodFrom = eventDateFrom,
            PeriodTo = eventDateTo,
            EventTypes = eventTypes,
        };
        var id = await _reportRepository.CreateReport(report);
        var message = new ReportCreateEvent(id, eventDateFrom, eventDateTo, eventTypes,report.Status);
        await _publisher.PublishAsync(_reportsExchangeName, _reportsRoutingKey, message);
        await _cacheService.InvalidateCache(_reportsCacheVersionKey);
        return report;
    }

    public async Task<IReadOnlyList<Report>> GetListOfReadyReports()
    {
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_reportsCacheVersionKey);
        var cacheKey = _cacheService.GenerateCacheKey(_reportsListCachePrefix, cacheVersion, null);
        var cachedResult = await _cacheService.GetAsync<IReadOnlyList<Report>>(cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var reports = await _reportRepository.GetReadyReports();

        await _cacheService.SetAsync(
            cacheKey,
            reports,
            TimeSpan.FromMinutes(_reportsListCacheTtlInMinutes));

        return reports;
        
    }

    public async Task<string> GetReportUrl(string reportName)
    {
        var (id,report) = await _reportRepository.GetReportByName(reportName);
        var generatedDate = report.GeneratedAt ?? DateTime.UtcNow;
        var fileName = $"{generatedDate.Year}/{generatedDate.Month}/{reportName}";
        var filePath = await _fileStorageService.GetFileLinkAsync(
           _reportsBucketName, fileName);
        report.FilePath = filePath;
        await _reportRepository.UpdateReport(id,report);
        return filePath;
    }
}