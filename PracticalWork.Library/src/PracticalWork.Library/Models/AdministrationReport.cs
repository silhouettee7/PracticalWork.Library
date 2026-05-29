
using PracticalWork.Library.Enums;

namespace PracticalWork.Library.Models;

public class AdministrationReport
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
    /// Статус готовности отчета
    /// </summary>
    public AdministrationReportStatus Status { get; set; } = AdministrationReportStatus.InProgress;

    public DateTime CreatedAt { get; set; }
}