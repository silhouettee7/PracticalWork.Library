using Domain.Abstractions.Services;
using Domain.Options;
using Microsoft.Extensions.Options;
using PracticalWork.Report.Abstractions.Services;
using PracticalWork.Report.Abstractions.Storage;
using PracticalWork.Report.Enums;
using PracticalWork.Report.Models;

namespace PracticalWork.Report.Application.Services;

public class ConsumerService: IConsumerService
{
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IReportRepository _reportRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IActivityReportGenerateService _activityReportGenerateService;
    private readonly ICacheService _cacheService;
    private readonly string _reportsCacheVersionKey;
    private readonly string _reportsBucketName;
    
    public ConsumerService(IReportRepository reportRepository,
        IActivityLogRepository activityLogRepository,
        ICacheService cacheService,
        IFileStorageService fileStorageService,
        IActivityReportGenerateService activityReportGenerateService,
        IOptionsMonitor<MinioOptions> minioOptions,
        IOptionsMonitor<RedisOptions> redisOptions)
    {
        _activityReportGenerateService = activityReportGenerateService;
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
            var reportResult = _activityReportGenerateService.GenerateReport(reportId, logs);
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