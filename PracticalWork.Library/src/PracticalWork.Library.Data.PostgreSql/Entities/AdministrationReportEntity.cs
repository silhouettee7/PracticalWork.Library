using Domain.Abstractions.Storage;
using PracticalWork.Library.Enums;

namespace PracticalWork.Library.Data.PostgreSql.Entities;

public class AdministrationReportEntity: EntityBase
{
    public string Name { get; set; }
    public string FilePath { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public AdministrationReportStatus Status { get; set; }
}