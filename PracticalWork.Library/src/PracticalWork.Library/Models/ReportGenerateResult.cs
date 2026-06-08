namespace PracticalWork.Library.Models;

/// <summary>
/// Объект результата сгенерированного отчета
/// </summary>
public class ReportGenerateResult
{
    /// <summary>
    /// Поток байтов, представляющий контент отчета
    /// </summary>
    public Stream Content { get; set; }
    /// <summary>
    /// Тип контента отчета
    /// </summary>
    public string ContentType { get; set; }
    /// <summary>
    /// Название файла отчета
    /// </summary>
    public string FileName { get; set; }
    /// <summary>
    /// Дата генерации
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}