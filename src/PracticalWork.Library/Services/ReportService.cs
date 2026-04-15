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
    private readonly TimeProvider _timeProvider;
    private readonly EmailMessagesOptions _emailMessagesOptions;
    private readonly BackgroundReportsOptions _backgroundReportsOptions;

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
        ILogger<ReportService> logger,
        TimeProvider timeProvider,
        IOptionsMonitor<EmailMessagesOptions> emailMessagesOptions, 
        IOptionsMonitor<BackgroundReportsOptions> backgroundReportsOptions)
    {
        _reportRepository = reportRepository;
        _cacheService = cacheService;
        _fileStorageService = fileStorageService;
        _publisher = publisher;
        _logger = logger;
        _timeProvider = timeProvider;
        _emailMessagesOptions = emailMessagesOptions.CurrentValue;
        _backgroundReportsOptions = backgroundReportsOptions.CurrentValue;
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
        var generatedDate = report.GeneratedAt ?? _timeProvider.GetUtcNow().UtcDateTime;
        var fileName = $"{generatedDate.Year}/{generatedDate.Month}/{reportName}";
        var filePath = await _fileStorageService.GetFileLinkAsync(
           _reportsBucketName, fileName);
        report.FilePath = filePath;
        await _reportRepository.UpdateReport(id,report);
        return filePath;
    }

    public async Task CreateReportForAdministration(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var booksStatistic = await GetBooksStatisticAsync(cancellationToken);
        
        var generatedReport = GenerateReport(booksStatistic);
        
        booksStatistic.FileUrl = await SaveReportAndGetFileUrlAsync(generatedReport, cancellationToken);

        var htmlTemplate = await GetEmailMessageHtmlBodyTemplateAsync(cancellationToken);
        
        foreach (var adminEmail in _adminEmails)
        {
            await SendWeeklyReportToAdmin(adminEmail, booksStatistic, 
                htmlTemplate, cancellationToken);
        }
    }

    private async Task<BooksStatistic> GetBooksStatisticAsync(CancellationToken cancellationToken)
    {
        var reportAdm = new BooksStatistic();
        
        var startDateTime = _timeProvider.GetUtcNow().UtcDateTime.Date.AddDays(-7);
        var endDateTime = _timeProvider.GetUtcNow().UtcDateTime.Date;
        
        reportAdm.AddedBooksCount = await _bookRepository
            .GetAddedBooksCount(startDateTime, endDateTime, cancellationToken);
        reportAdm.RegisterReadersCount = await _readerRepository
            .GetNewReadersCount(startDateTime, endDateTime, cancellationToken);
        
        var startDate = DateOnly.FromDateTime(startDateTime);
        var endDate = DateOnly.FromDateTime(endDateTime);
        
        var borrowedStatistic = await _borrowRepository
            .GetBorrowBookStatistic(startDate, endDate, cancellationToken);
        
        reportAdm.BorrowedCount = borrowedStatistic.BorrowedCount;
        reportAdm.ReturnedCount = borrowedStatistic.ReturnedCount;
        reportAdm.OverdueCount = borrowedStatistic.OverdueCount;
        reportAdm.PeriodFrom = startDate;
        reportAdm.PeriodTo = endDate.AddDays(-1);

        return reportAdm;
    }

    private ReportGenerateResult GenerateReport(BooksStatistic booksStatistic)
    {
        var reportName = _backgroundReportsOptions.ReportForAdministration;
        var reportFileName = $"{reportName}_{_timeProvider.GetUtcNow().UtcDateTime:yyyy-MM-dd}.csv";
        
        var generatedReport = _reportGenerateService.GenerateReport([booksStatistic], reportFileName);
        
        booksStatistic.GeneratedAt = generatedReport.GeneratedAt;
        
        _logger.LogInformation("Отчет для администрации - {ReportName} сформирован", reportFileName);
        
        return generatedReport;
    }

    private async Task<string> SaveReportAndGetFileUrlAsync(ReportGenerateResult generatedReport, 
        CancellationToken cancellationToken)
    {
        await _fileStorageService.UploadFileAsync(_reportsAdministrationBucketName, 
            generatedReport.FileName, generatedReport.Content, 
            generatedReport.ContentType, cancellationToken);
        await _fileStorageService.SetBucketFilesLifeTimeAsync(
            _reportsAdministrationBucketName, _timeProvider.GetUtcNow().UtcDateTime.AddDays(90), 
            $"{_timeProvider.GetUtcNow().UtcDateTime:MM-dd}", cancellationToken);
        
        _logger.LogInformation("Отчет для администрации - {ReportName} загружен", generatedReport.FileName);
        
        var reportUrl = await _fileStorageService.GetFileLinkAsync(
            _reportsAdministrationBucketName, generatedReport.FileName, cancellationToken);
        
        var report = new Report
        {
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            Status = ReportStatus.Generated,
            Name = generatedReport.FileName,
            GeneratedAt = generatedReport.GeneratedAt,
            FilePath = reportUrl
        };
        await _reportRepository.SaveReportAsync(report, cancellationToken);
        
        _logger.LogInformation("Отчет для администрации - {ReportName} сохранен", report.Name);

        return report.FilePath;
    }
    
    private async Task<string> GetEmailMessageHtmlBodyTemplateAsync(CancellationToken cancellationToken)
    {
        var fileName = _emailMessagesOptions.ReportForAdministration.TemplateFileName;
        var htmlTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), fileName); 
        
        return await File.ReadAllTextAsync(htmlTemplatePath, cancellationToken);
    }

    private async Task SendWeeklyReportToAdmin(string adminEmail, BooksStatistic booksStatistic, 
        string htmlTemplate, CancellationToken cancellationToken)
    {
        var subject = _emailMessagesOptions.ReportForAdministration.Subject;
        var email = new EmailMessage(adminEmail, subject, htmlTemplate, true);
        email.Personalize(booksStatistic);
        
        var sentResult = await _emailService.SendAsync(email, cancellationToken);
        
        if (sentResult.IsSuccess)
        {
            _logger.LogInformation("Отчет успешно отправлен для администратора: {adminEmail}", adminEmail);
        }
        else
        {
            _logger.LogError("Ошибка отправки отчета администратору: {adminEmail}", adminEmail);
        }
    }
}