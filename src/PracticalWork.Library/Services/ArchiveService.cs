using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
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

    public ArchiveService(IBookRepository bookRepository, 
        IBookService bookService,
        IReportGenerateService reportGenerateService,
        ILogger<ArchiveService> logger,
        IFileStorageService fileStorageService,
        OptionsMonitor<MinioOptions> minioOptions, 
        IReportRepository reportRepository)
    {
        _bookRepository = bookRepository;
        _bookService = bookService;
        _reportGenerateService = reportGenerateService;
        _logger = logger;
        _fileStorageService = fileStorageService;
        _reportRepository = reportRepository;
        _minioOptions = minioOptions.CurrentValue;
    }
    
    public async Task ArchiveOldBooksAsync()
    {
        var archiveLog = new ArchiveLog();
        var wrongReasons = new HashSet<string>();
        var stopWatch = Stopwatch.StartNew();
        var pagination = new CursorPaginationRequest
        {
            PageSize = 100,
            Forward = true
        };
        var books = await _bookRepository.GetAvailableOldBooksPage(pagination);
        foreach (var book in books)
        {
            try
            {
                await _bookService.ArchiveBook(book.Id);
                archiveLog.SuccessCount++;
                _logger.LogInformation("Книга с id:{Id} архивирована",book.Id);
            }
            catch (Exception ex)
            {
                archiveLog.WrongCount++;
                wrongReasons.Add(ex.Message);
                _logger.LogError(ex, "Архивация книга с id:{Id} не удалась",book.Id);
            }
            archiveLog.TotalCount++;
        }
        archiveLog.WrongReasons = string.Join(";\n", wrongReasons);
        stopWatch.Stop();
        archiveLog.TotalTime = $"{stopWatch.Elapsed:hh\\:mm\\:ss\\.fff}";
        _logger.LogInformation("Архивация прошла\n" +
                               "Всего:{TotalCount}\n" +
                               "Успешных:{SuccessCount}\n" +
                               "Пропущенных:{WrongCount}\n" +
                               "Время:{TotalTime}\n}", 
            archiveLog.TotalCount, archiveLog.SuccessCount, archiveLog.WrongCount, archiveLog.TotalTime);
        var reportName = "Архивация_старых_книг";
        var timestamp = DateTime.UtcNow;
        string fileName = $"{timestamp.Year}/{reportName}_{timestamp.Month}.csv";
        var result = _reportGenerateService.GenerateReport([archiveLog], fileName);
        var reportSave = new Report()
        {
            CreatedAt = DateTime.UtcNow,
            GeneratedAt = DateTime.UtcNow,
            Status = ReportStatus.Generated,
            Name = fileName
        };
        await _fileStorageService.UploadFileAsync(
            _minioOptions.ArchiveBooksBucketName, result.FileName, result.Content, result.ContentType);
        var filePath = await _fileStorageService.GetFileLinkAsync(
            _minioOptions.ArchiveBooksBucketName,fileName);
        reportSave.FilePath = filePath;
        await _reportRepository.SaveReportAsync(reportSave);
    }
}