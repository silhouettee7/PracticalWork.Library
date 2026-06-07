using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Attributes;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Application.Services;

public class ReportGenerateService: IReportGenerateService
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReportGenerateService> _logger;

    public ReportGenerateService(
        TimeProvider timeProvider,
        ILogger<ReportGenerateService> logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public ReportGenerateResult GenerateReport<T>(IEnumerable<T> items, string fileName)
    {
        var csv = new StringBuilder();
        var tableColumns = typeof(T).GetProperties()
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttribute<TableColumnAttribute>()
            })
            .Where(p => p.Attribute is not null)
            .OrderBy(x => x.Attribute.Order)
            .ToList();
        
        var headers = tableColumns.Select(x => x.Attribute.Name);
        csv.AppendLine(string.Join(";", headers));
        
        foreach (var item in items)
        {
            var row = new List<string>(tableColumns.Count);
            foreach (var property in tableColumns)
            {
                var val = property.Property.GetValue(item);
                var stringValue = val?.ToString() ?? string.Empty;
                row.Add(stringValue);
            }
            csv.AppendLine(string.Join(";", row));
        }
        
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));
        string contentType = "text/csv";
        
        return new ReportGenerateResult
        {
            FileName = fileName,
            Content = stream,
            ContentType = contentType,
            GeneratedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };
    }
    
    public ReportGenerateResult GenerateReportForAdministration(string reportName, BooksStatistic booksStatistic)
    {
        var reportFileName = $"{reportName}_{_timeProvider.GetUtcNow().UtcDateTime:yyyy-MM-dd}.csv";
        
        var generatedReport = GenerateReport([booksStatistic], reportFileName);
        
        booksStatistic.GeneratedAt = generatedReport.GeneratedAt;
        
        _logger.LogInformation("Отчет для администрации - {ReportName} сформирован", reportFileName);
        
        return generatedReport;
    }

    public ReportGenerateResult GenerateArchiveReport(string reportName, ArchiveLog archiveLog)
    {
        var timestamp = _timeProvider.GetUtcNow().UtcDateTime;
        string fileName = $"{timestamp.Year}/{reportName}_{timestamp.Month}.csv";
        return GenerateReport([archiveLog], fileName);
    }
}