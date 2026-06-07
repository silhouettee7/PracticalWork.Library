using Domain.Abstractions.Services;
using Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Application.Services;

public class AdministrationReportService: IAdministrationReportService
{
    private readonly IBookRepository _bookRepository;
    private readonly IBorrowRepository _borrowRepository;
    private readonly IReaderRepository _readerRepository;
    private readonly IAdministrationReportRepository _administrationReportRepository;
    private readonly IReportGenerateService _reportGenerateService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IEmailService _emailService;
    private readonly IEmailMessageTemplateService _emailMessageTemplateService;
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
        TimeProvider timeProvider, 
        IAdministrationReportRepository administrationReportRepository, 
        IEmailMessageTemplateService emailMessageTemplateService)
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
        _emailMessageTemplateService = emailMessageTemplateService;
    }
    
    public async Task CreateReportForAdministration(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var booksStatistic = await GetBooksStatisticAsync(cancellationToken);
        
        var generatedReport = _reportGenerateService.GenerateReportForAdministration(
            _backgroundReportsOptions.ReportForAdministration, booksStatistic);
        
        booksStatistic.FileUrl = await SaveReportAndGetFileUrlAsync(generatedReport, cancellationToken);

        var htmlTemplate = await _emailMessageTemplateService.GetEmailMessageHtmlBodyTemplateAsync(
            _emailMessagesOptions.ReportForAdministration.TemplateFileName, cancellationToken);
        
        foreach (var adminEmail in _adminEmails)
        {
            try
            {
                await _emailService.SendWeeklyReportToAdmin(
                    _emailMessagesOptions.ReportForAdministration.Subject,
                    adminEmail, booksStatistic,
                    htmlTemplate, cancellationToken);
            }
            catch (EmailServiceException)
            {
                _logger.LogError("Администратор {adminEmail} не получил письма. Отправка не прерывается", adminEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла неизвестная ошибка при отправке сообщения. Отправка не прерывается");
            }
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

        if (borrowedStatistic is null)
        {
            _logger.LogInformation("Нет статистики по выдачам за указанный период: {startDate} - {endDate}", startDate, endDate);
        }
        
        reportAdm.BorrowedCount = borrowedStatistic?.BorrowedCount ?? 0;
        reportAdm.ReturnedCount = borrowedStatistic?.ReturnedCount ?? 0;
        reportAdm.OverdueCount = borrowedStatistic?.OverdueCount ?? 0;
        reportAdm.PeriodFrom = startDate;
        reportAdm.PeriodTo = endDate.AddDays(-1);

        return reportAdm;
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
            Status = AdministrationReportStatus.Generated,
            Name = generatedReport.FileName,
            GeneratedAt = generatedReport.GeneratedAt,
            FilePath = reportUrl
        };
        await _administrationReportRepository.SaveReportAsync(report, cancellationToken);
        
        _logger.LogInformation("Отчет для администрации - {ReportName} сохранен", report.Name);

        return report.FilePath;
    }
}