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
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly ICursorPaginationService<ActivityLog> _cursorPaginationService;
    private readonly IReportRepository _reportRepository;
    private readonly IRabbitMQPublisher _publisher;
    private readonly IFileStorageService _fileStorageService;
    private readonly IReportGenerateService _reportGenerateService;
    private readonly ICacheService _cacheService;
    private readonly MinioOptions _minioOptions;
    private readonly string _exchangeName;
    private readonly string _routingKey;
    private readonly string _cacheVersion;
    private readonly string _reportsListPrefix;
    private readonly double _reportsListTtlInMinutes;
    
    public ReportService(IActivityLogRepository activityLogRepository,
        ICursorPaginationService<ActivityLog> cursorPaginationService,
        IReportRepository reportRepository,
        IRabbitMQPublisher publisher,
        IConfiguration configuration,
        ICacheService cacheService,
        IFileStorageService fileStorageService,
        IOptionsMonitor<MinioOptions> minioOptions,
        IReportGenerateService reportGenerateService)
    {
        _activityLogRepository = activityLogRepository;
        _cursorPaginationService = cursorPaginationService;
        _reportGenerateService = reportGenerateService;
        _reportRepository = reportRepository;
        _cacheService = cacheService;
        _fileStorageService = fileStorageService;
        _publisher = publisher;
        _minioOptions = minioOptions.CurrentValue;
        var rabbitSection = configuration.GetSection("App:RabbitMQ:Reports");
        _exchangeName = rabbitSection["Exchange"];
        _routingKey = rabbitSection["RoutingKey"];
        var redisSection = configuration.GetSection("App:Redis:Reports");
        _cacheVersion = redisSection["VersionKey"];
        _reportsListPrefix = redisSection["ReportsList:Prefix"];
        _reportsListTtlInMinutes = redisSection.GetValue<double>("ReportsList:TtlInMinutes");
    }

    public async Task WriteSystemActivityLogs(ActivityLog log)
    {
        await _activityLogRepository.AddLogAsync(log);
    }

    public async Task<CursorPaginationResponse<ActivityLog>> ReadSystemActivityLogs(CursorPaginationRequest request, 
        string[] eventTypes, DateOnly? eventDateFrom, DateOnly? eventDateTo)
    {
        var logs = await _activityLogRepository
            .GetLogsPageAsync(request, eventDateFrom, eventDateTo, eventTypes);
        return _cursorPaginationService.ToCursorPageResponse(logs, request);
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
        await _publisher.PublishAsync(_exchangeName, _routingKey, message);
        await _cacheService.InvalidateCache(_cacheVersion);
        return report;
    }

    public async Task GenerateReport(Guid reportId, DateOnly? periodFrom, 
        DateOnly? periodTo, string[] eventTypes)
    {
        var report = await _reportRepository.GetReportById(reportId);
        var logs = await _activityLogRepository.GetLogsAsync(
            periodFrom, periodTo, eventTypes);
        try
        {
            var reportResult = _reportGenerateService.GenerateReport(reportId, logs);
            await _fileStorageService.UploadFileAsync(_minioOptions.ReportsBucketName,
                reportResult.FileName, reportResult.Content, reportResult.ContentType);
            var fileName = reportResult.FileName.Split('/')[^1];
            report.MarkAsGenerated(fileName);
            await _reportRepository.UpdateReport(reportId,report);
            await _cacheService.InvalidateCache(_cacheVersion);
        }
        catch (Exception)
        {
            report.Status = ReportStatus.Error;
            await _reportRepository.UpdateReport(reportId,report);
            await _cacheService.InvalidateCache(_cacheVersion);
            throw;
        }
    }

    public async Task<IReadOnlyList<Report>> GetListOfReadyReports()
    {
        var cacheVersion = await _cacheService.GetCurrentCacheVersion(_cacheVersion);
        var cacheKey = _cacheService.GenerateCacheKey(_reportsListPrefix, cacheVersion, null);
        var cachedResult = await _cacheService.GetAsync<IReadOnlyList<Report>>(cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var reports = await _reportRepository.GetReadyReports();

        await _cacheService.SetAsync(
            cacheKey,
            reports,
            TimeSpan.FromMinutes(_reportsListTtlInMinutes));

        return reports;
        
    }

    public async Task<string> GetReportUrl(string reportName)
    {
        var (id,report) = await _reportRepository.GetReportByName(reportName);
        var generatedDate = report.GeneratedAt ?? DateTime.UtcNow;
        var fileName = $"{generatedDate.Year}/{generatedDate.Month}/{reportName}";
        var filePath = await _fileStorageService.GetFileLinkAsync(
            _minioOptions.ReportsBucketName, fileName);
        report.FilePath = filePath;
        await _reportRepository.UpdateReport(id,report);
        return filePath;
    }
}