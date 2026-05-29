namespace PracticalWork.Library.Enums;

/// <summary>
/// Статус готовности отчета для администрации
/// </summary>
public enum AdministrationReportStatus
{
    /// <summary>
    /// В процессе
    /// </summary>
    InProgress = 0,
    /// <summary>
    /// Готов
    /// </summary>
    Generated = 10,
    /// <summary>
    /// Генерация с ошибкой
    /// </summary>
    Error = 20,
}