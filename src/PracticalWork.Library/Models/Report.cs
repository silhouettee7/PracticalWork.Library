using PracticalWork.Library.Enums;

namespace PracticalWork.Library.Models;

/// <summary>
/// Отчет с записями событий системы
/// </summary>
public class Report
{
    /// <summary>
    /// Название отчета(файла) 
    /// </summary>
    public string Name { get; set; } 
    /// <summary>
    /// Путь, по которому можно получить файл
    /// </summary>
    public string FilePath { get;  set; }
    /// <summary>
    /// Когда был сгенерирован отчет
    /// </summary>
    public DateTime? GeneratedAt { get; set; }
    /// <summary>
    /// Фильтр даты, с которой начинаются записи
    /// </summary>
    public DateOnly? PeriodFrom { get; set; }
    /// <summary>
    /// Фильтр даты, которой заканчиваются записи
    /// </summary>
    public DateOnly? PeriodTo { get; set;  }
    /// <summary>
    /// Фильтр на типы событий, которые есть в отчете
    /// </summary>
    public IReadOnlyList<string> EventTypes { get; set; }
    /// <summary>
    /// Статус готовности отчета
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.InProgress;
    
    /// <summary>
    /// Пометить отчет как сгенерированный
    /// </summary>
    /// <param name="fileName">название отчета</param>
    public void MarkAsGenerated(string fileName)
    {
        GeneratedAt = DateTime.UtcNow;
        Status = ReportStatus.Generated;
        Name = fileName;
    }
}