using PracticalWork.Library.Abstractions.Storage;

namespace PracticalWork.Library.Reports.PostgreSql.Entities;

public class ActivityLogEntity: EntityBase
{
    public Guid? BookId { get; set; }
    public Guid? ReaderId { get; set; }
    public string EventType { get; set; } = null!;
    public DateTime EventDate { get; set; }
    public string? Metadata { get; set; }
}