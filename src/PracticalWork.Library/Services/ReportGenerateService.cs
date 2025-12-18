using System.Text;
using System.Text.Json;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Services;

public class ReportGenerateService: IReportGenerateService
{
    public ReportGenerateResult GenerateReport(Guid reportId, IReadOnlyList<ActivityLog> logs)
    {
        var timestamp = DateTime.UtcNow;
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