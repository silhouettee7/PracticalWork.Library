using Domain.Abstractions.Services;
using Domain.Enums;
using Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Services;

public class AdministrationReportService: IAdministrationReportService
{
    private readonly IBookRepository _bookRepository;
    private readonly IBorrowRepository _borrowRepository;
    private readonly IReaderRepository _readerRepository;
    private readonly IAdministrationReportRepository _administrationReportRepository;
    private readonly IReportGenerateService _reportGenerateService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AdministrationReportService> _logger;
    private readonly string _reportsAdministrationBucketName;
    private readonly IReadOnlyList<string> _adminEmails;
    private readonly EmailMessagesOptions _emailMessagesOptions;
    private readonly BackgroundReportsOptions _backgroundReportsOptions;
    private readonly TimeProvider _timeProvider;

    public AdministrationReportService(
        IBookRepository bookRepository,
        IBorrowRepository borrowRepository,
        IReaderRepository readerRepository,
        IEmailService emailService,
        IFileStorageService fileStorageService,
        IOptionsMonitor<EmailOptions> emailOptions,
        IOptionsMonitor<EmailMessagesOptions> emailMessagesOptions, 
        IOptionsMonitor<BackgroundReportsOptions> backgroundReportsOptions,
        IReportGenerateService reportGenerateService,
        ILogger<AdministrationReportService> logger,
        IOptionsMonitor<MinioOptions> minioOptions,
        TimeProvider timeProvider, IAdministrationReportRepository administrationReportRepository)
    {
        _emailMessagesOptions = emailMessagesOptions.CurrentValue;
        _backgroundReportsOptions = backgroundReportsOptions.CurrentValue;
        _reportGenerateService = reportGenerateService;
        _emailService = emailService;
        _logger = logger;
        
        var minioOpt = minioOptions.CurrentValue;
        
        _reportsAdministrationBucketName = minioOpt.ReportsAdministrationBucketName;
        _adminEmails = emailOptions.CurrentValue.AdminEmails;
        _bookRepository = bookRepository;
        _borrowRepository = borrowRepository;
        _readerRepository = readerRepository;
        _fileStorageService = fileStorageService;
        _timeProvider = timeProvider;
        _administrationReportRepository = administrationReportRepository;
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
        
        var report = new AdministrationReport
        {
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            Status = ReportStatus.Generated,
            Name = generatedReport.FileName,
            GeneratedAt = generatedReport.GeneratedAt,
            FilePath = reportUrl
        };
        await _administrationReportRepository.SaveReportAsync(report, cancellationToken);
        
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