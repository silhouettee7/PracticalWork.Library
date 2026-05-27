using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Services;

public class ConsumerService: IConsumerService
{
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IReportRepository _reportRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IReportGenerateService _reportGenerateService;
    private readonly ICacheService _cacheService;
    private readonly string _reportsCacheVersionKey;
    private readonly string _reportsBucketName;
    
    public ConsumerService(IReportRepository reportRepository,
        IActivityLogRepository activityLogRepository,
        ICacheService cacheService,
        IFileStorageService fileStorageService,
        IReportGenerateService reportGenerateService,
        IOptionsMonitor<MinioOptions> minioOptions,
        IOptionsMonitor<RedisOptions> redisOptions)
    {
        _reportGenerateService = reportGenerateService;
        _reportRepository = reportRepository;
        _activityLogRepository = activityLogRepository;
        _cacheService = cacheService;
        _fileStorageService = fileStorageService;
        var minioOpt = minioOptions.CurrentValue;
        var redisOpt = redisOptions.CurrentValue;
        
        _reportsCacheVersionKey = redisOpt.Reports.VersionKey;
        _reportsBucketName = minioOpt.ReportsBucketName;
    }
    public async Task WriteSystemActivityLogs(ActivityLog log)
    {
        await _activityLogRepository.AddLogAsync(log);
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
            await _fileStorageService.UploadFileAsync(_reportsBucketName,
                reportResult.FileName, reportResult.Content, reportResult.ContentType);
            var fileName = reportResult.FileName.Split('/')[^1];
            report.MarkAsGenerated(fileName);
            await _reportRepository.UpdateReport(reportId,report);
            await _cacheService.InvalidateCache(_reportsCacheVersionKey);
        }
        catch (Exception)
        {
            report.Status = ReportStatus.Error;
            await _reportRepository.UpdateReport(reportId,report);
            await _cacheService.InvalidateCache(_reportsCacheVersionKey);
            throw;
        }
    }
}