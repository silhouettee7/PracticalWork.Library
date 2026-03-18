using Microsoft.Extensions.Logging;
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
    private readonly IBookRepository _bookRepository;
    private readonly IBorrowRepository _borrowRepository;
    private readonly IReaderRepository _readerRepository;
    private readonly IRabbitMqPublisher _publisher;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICacheService _cacheService;
    private readonly IReportGenerateService _reportGenerateService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReportService> _logger;
    private readonly string _reportsExchangeName;
    private readonly string _reportsRoutingKey;
    private readonly string _reportsCacheVersionKey;
    private readonly string _reportsListCachePrefix;
    private readonly double _reportsListCacheTtlInMinutes;
    private readonly string _reportsBucketName;
    private readonly string _reportsAdministrationBucketName;
    private readonly IReadOnlyList<string> _adminEmails;

    public ReportService(
        IReportRepository reportRepository,
        IBookRepository bookRepository,
        IBorrowRepository borrowRepository,
        IReaderRepository readerRepository,
        IRabbitMqPublisher publisher,
        ICacheService cacheService,
        IFileStorageService fileStorageService,
        IReportGenerateService reportGenerateService,
        IEmailService emailService,
        IOptionsMonitor<MinioOptions> minioOptions,
        IOptionsMonitor<RedisOptions> redisOptions,
        IOptionsMonitor<RabbitOptions> rabbitOptions,
        IOptionsMonitor<EmailOptions> emailOptions,
        ILogger<ReportService> logger)
    {
        _reportRepository = reportRepository;
        _cacheService = cacheService;
        _fileStorageService = fileStorageService;
        _publisher = publisher;
        _logger = logger;
        _reportGenerateService = reportGenerateService;
        _emailService = emailService;
        var minioOpt = minioOptions.CurrentValue;
        var redisOpt = redisOptions.CurrentValue;
        var rabbitOpt = rabbitOptions.CurrentValue;
        
        _reportsExchangeName = rabbitOpt.Reports.Exchange;
        _reportsRoutingKey = rabbitOpt.Reports.RoutingKey;
        _reportsCacheVersionKey = redisOpt.Reports.VersionKey;
        _reportsListCachePrefix = redisOpt.Reports.ReportsList.Prefix;
        _reportsListCacheTtlInMinutes = redisOpt.Reports.ReportsList.TtlInMinutes;
        _reportsBucketName = minioOpt.ReportsBucketName;
        _reportsAdministrationBucketName = minioOpt.ReportsAdministrationBucketName;
        _adminEmails = emailOptions.CurrentValue.AdminEmails;
        _bookRepository = bookRepository;
        _borrowRepository = borrowRepository;
        _readerRepository = readerRepository;
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

    public async Task CreateReportForAdministration()
    {
        var reportAdm = new ReportForAdministration();
        var startDateTime = DateTime.UtcNow.Date.AddDays(-7);
        var endDateTime = DateTime.UtcNow.Date;
        reportAdm.AddedBooksCount = await _bookRepository.GetAddedBooksCount(startDateTime, endDateTime);
        reportAdm.RegisterReadersCount = await _readerRepository.GetNewReadersCount(startDateTime, endDateTime);
        var startDate = DateOnly.FromDateTime(startDateTime);
        var endDate = DateOnly.FromDateTime(endDateTime);
        var borrowedStatistic = await _borrowRepository.GetBorrowBookStatistic(startDate, endDate);
        reportAdm.BorrowedCount = borrowedStatistic.BorrowedCount;
        reportAdm.ReturnedCount = borrowedStatistic.ReturnedCount;
        reportAdm.OverdueCount = borrowedStatistic.OverdueCount;
        _logger.LogInformation("Информация по отчету для администрации получена");
        var generatedReport = _reportGenerateService
            .GenerateReport([reportAdm], $"report_{DateTime.UtcNow:yyyy-MM-dd}.csv");
        var report = new Report
        {
            CreatedAt = DateTime.UtcNow,
            Status = ReportStatus.Generated,
            Name = $"Отчет_для_администрации_{DateTime.UtcNow:yyyy-MM-dd}",
            GeneratedAt = DateTime.UtcNow
        };
        _logger.LogInformation("Отчет для администрации сформирован");
        await _fileStorageService.UploadFileAsync(_reportsAdministrationBucketName, 
            generatedReport.FileName, generatedReport.Content, generatedReport.ContentType);
        await _fileStorageService.SetBucketFilesLifeTimeAsync(
            _reportsAdministrationBucketName, DateTime.UtcNow.AddDays(90), $"{DateTime.UtcNow:MM-dd}" );
        _logger.LogInformation("Отчет для администрации загружен");
        var reportUrl = await _fileStorageService.GetFileLinkAsync(
            _reportsAdministrationBucketName, generatedReport.FileName);
        report.FilePath = reportUrl;
        await _reportRepository.SaveReportAsync(report);
        _logger.LogInformation("Отчет для администрации сохранен");
        reportAdm.GeneratedAt = report.GeneratedAt.Value;
        reportAdm.FileUrl = reportUrl;
        reportAdm.PeriodFrom = startDate;
        reportAdm.PeriodTo = endDate.AddDays(-1);
        var htmlTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "report_for_admins.html"); 
        var htmlTemplate = await File.ReadAllTextAsync(htmlTemplatePath);
        foreach (var adminEmail in _adminEmails)
        {
            var email = new EmailMessage(adminEmail, "Еженедельный отчет библиотеки", 
                htmlTemplate, true);
            email.Personalize(reportAdm);
            var sentResult = await _emailService.SendAsync(email);
            if (!sentResult.IsSuccess)
            {
                _logger.LogError("Ошибка отправки письма отчета администратору:{adminEmail}", adminEmail);
            }
            else
            {
                _logger.LogInformation("Письмо успешно отправлено для {adminEmail}", adminEmail);
            }
        }
    }
}