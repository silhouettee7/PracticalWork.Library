using System.Text;
using System.Text.Json;
using PracticalWork.Report.Abstractions.Services;
using PracticalWork.Report.Models;

namespace PracticalWork.Report.Application.Services;

public class ActivityReportGenerateService: IActivityReportGenerateService
{
    private readonly TimeProvider _timeProvider;

    public ActivityReportGenerateService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public ReportGenerateResult GenerateReport(Guid reportId, IReadOnlyList<ActivityLog> logs)
    {
        var timestamp = _timeProvider.GetUtcNow().UtcDateTime;
        string fileName = $"{timestamp.Year}/{timestamp.Month}/{reportId}.csv";
        string contentType = "text/csv";

        var sb = new StringBuilder();
        
        sb.AppendLine("EventType;EventDate;Metadata");
        foreach (var log in logs)
        {
            sb.AppendLine($"{log.EventType};{log.EventDate};{JsonSerializer.Serialize(log, log.GetType())}");
        }
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));

        return new ReportGenerateResult
        {
            FileName = fileName,
            Content = stream,
            ContentType = contentType
        };
    }
}