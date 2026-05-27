namespace PracticalWork.Library.Contracts.v1.Enums;

/// <summary>
/// Состояние книги
/// </summary>
public enum BookIssueStatus
{
    /// <summary>
    /// Выдана
    /// </summary>
    /// <remarks>Значение по умолчанию</remarks>
    Issued = 0,

    /// <summary>
    /// Возвращена
    /// </summary>
    Returned = 10,

    /// <summary>
    /// Возвращена с просрочкой
    /// </summary>
    Overdue = 20
}