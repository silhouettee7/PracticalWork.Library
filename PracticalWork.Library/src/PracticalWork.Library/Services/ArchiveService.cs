using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Dtos;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Services;

public class ArchiveService: IArchiveService
{
    private readonly IBookRepository _bookRepository;
    private readonly IReportRepository _reportRepository;
    private readonly IBookService _bookService;
    private readonly ILogger<ArchiveService> _logger;
    private readonly IReportGenerateService _reportGenerateService;
    private readonly IFileStorageService _fileStorageService;
    private readonly MinioOptions _minioOptions;
    private readonly TimeProvider _timeProvider;
    private readonly BackgroundReportsOptions _reportsOptions;

    public ArchiveService(IBookRepository bookRepository, 
        IBookService bookService,
        IReportGenerateService reportGenerateService,
        ILogger<ArchiveService> logger,
        IFileStorageService fileStorageService,
        IOptionsMonitor<MinioOptions> minioOptions, 
        IReportRepository reportRepository,
        TimeProvider timeProvider, 
        IOptionsMonitor<BackgroundReportsOptions> reportsOptions)
    {
        _bookRepository = bookRepository;
        _bookService = bookService;
        _reportGenerateService = reportGenerateService;
        _logger = logger;
        _fileStorageService = fileStorageService;
        _reportRepository = reportRepository;
        _minioOptions = minioOptions.CurrentValue;
        _timeProvider = timeProvider;
        _reportsOptions = reportsOptions.CurrentValue;
    }
    
    public async Task ArchiveOldBooksAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var stopWatch = Stopwatch.StartNew();

        var books = await GetOldBooksAsync(cancellationToken);
        var archiveLog = await ArchiveOldBooksAsync(books, cancellationToken);
        
        stopWatch.Stop();
        archiveLog.TotalTime = $"{stopWatch.Elapsed:hh\\:mm\\:ss\\.fff}";
        
        _logger.LogInformation("Архивация прошла\n" +
                               "Всего:{TotalCount}\n" +
                               "Успешных:{SuccessCount}\n" +
                               "Пропущенных:{WrongCount}\n" +
                               "Время:{TotalTime}\n", 
            archiveLog.TotalCount, archiveLog.SuccessCount, archiveLog.WrongCount, archiveLog.TotalTime);
        
        var report = GenerateReport(archiveLog);
        await SaveReportAsync(report, cancellationToken);
    }

    private async Task<List<AvailableOldBookDto>> GetOldBooksAsync(CancellationToken cancellationToken)
    {
        var pagination = new CursorPaginationRequest
        {
            PageSize = 100,
            Forward = true
        };
        var dateThreeYearsAgo = DateOnly.FromDateTime(
            _timeProvider.GetUtcNow().UtcDateTime.AddYears(-3));
        return await _bookRepository
            .GetAvailableOldBooksPage(dateThreeYearsAgo, pagination, cancellationToken);
    }

    private async Task<ArchiveLog> ArchiveOldBooksAsync(
        List<AvailableOldBookDto> books, CancellationToken cancellationToken)
    {
        var archiveLog = new ArchiveLog();
        var wrongReasons = new HashSet<string>();
        
        foreach (var book in books)
        {
            try
            {
                await _bookService.ArchiveBook(book.Id, cancellationToken);
                archiveLog.SuccessCount++;
                _logger.LogInformation("Книга с id:{Id} архивирована",book.Id);
            }
            catch (Exception ex)
            {
                archiveLog.WrongCount++;
                wrongReasons.Add(ex.Message);
                _logger.LogError(ex, "Архивация книга с id:{Id} не удалась " +
                                     "по причине: {Reason}",book.Id, ex.Message);
            }
            archiveLog.TotalCount++;
        }
        archiveLog.WrongReasons = string.Join(";\n", wrongReasons);
        
        return archiveLog;
    }

    private ReportGenerateResult GenerateReport(ArchiveLog archiveLog)
    {
        var reportName = _reportsOptions.ReportAboutArchive;
        var timestamp = _timeProvider.GetUtcNow().UtcDateTime;
        string fileName = $"{timestamp.Year}/{reportName}_{timestamp.Month}.csv";
        return _reportGenerateService.GenerateReport([archiveLog], fileName);
    }

    private async Task SaveReportAsync(ReportGenerateResult report, CancellationToken cancellationToken)
    {
        await _fileStorageService.UploadFileAsync(
            _minioOptions.ArchiveBooksBucketName, report.FileName, report.Content, report.ContentType, cancellationToken);
        var filePath = await _fileStorageService.GetFileLinkAsync(
            _minioOptions.ArchiveBooksBucketName,report.FileName, cancellationToken);
        
        var reportSave = new Report
        {
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            GeneratedAt = _timeProvider.GetUtcNow().UtcDateTime,
            Status = ReportStatus.Generated,
            Name = report.FileName,
            FilePath = filePath
        };
        await _reportRepository.SaveReportAsync(reportSave, cancellationToken);
    }
}