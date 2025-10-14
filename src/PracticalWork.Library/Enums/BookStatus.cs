namespace PracticalWork.Library.Enums;

/// <summary>
/// Статус книги в библиотеке
/// </summary>
public enum BookStatus
{
    /// <summary>Книга доступна для выдачи</summary>
    /// <remarks>Значение по умолчанию</remarks>
    Available = 0,

    /// <summary>Книга выдана читателю</summary>
    Borrow = 10,

    /// <summary>Книга переведена в архив</summary>
    Archived = 20
}