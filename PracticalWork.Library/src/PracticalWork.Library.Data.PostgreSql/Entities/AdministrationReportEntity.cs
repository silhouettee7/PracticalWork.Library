using Domain.Abstractions.Storage;
using Domain.Enums;

namespace PracticalWork.Library.Data.PostgreSql.Entities;

public class AdministrationReportEntity: EntityBase
{
    public string Name { get; set; }
    public string FilePath { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public ReportStatus Status { get; set; }
}