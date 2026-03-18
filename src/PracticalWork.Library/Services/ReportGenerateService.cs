using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;
using System.Text.Json;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Attributes;
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

    public ReportGenerateResult GenerateReport<T>(IEnumerable<T> items, string fileName)
    {
        var csv = new StringBuilder();
        var properties = typeof(T).GetProperties()
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttribute<TableColumnAttribute>() 
                            ?? new TableColumnAttribute(p.Name)
            })
            .OrderBy(x => x.Attribute.Order)
            .ToList();
        var headers = properties.Select(x => x.Attribute.Name);
        csv.AppendLine(string.Join(";", headers));
        foreach (var item in items)
        {
            var row = new List<string>(properties.Count);
            foreach (var property in properties)
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
            ContentType = contentType
        };
    }
}