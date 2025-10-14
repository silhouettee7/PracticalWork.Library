namespace PracticalWork.Library.Data.PostgreSql.Entities;

/// <summary>
/// Научная литература
/// </summary>
public sealed class ScientificBookEntity : AbstractBookEntity
{
    /// <summary>Область исследований</summary>
    public string ResearchField { get; set; }

    /// <summary>Издатель</summary>
    public string Publisher { get; set; }
}