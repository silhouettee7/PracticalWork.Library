namespace PracticalWork.Library.Models;

public sealed class Reader
{
    /// <summary>ФИО</summary>
    /// <remarks>Запись идет через пробел</remarks>
    public string FullName { get; set; }

    /// <summary>Номер телефона</summary>
    public string PhoneNumber { get; set; }

    /// <summary>Дата окончания действия карточки</summary>
    public DateOnly ExpiryDate { get; set; }

    /// <summary>Активность карточки</summary>
    public bool IsActive { get; set; }
    /// <summary>
    /// Выданные читателю книги
    /// </summary>
    public IReadOnlyList<Book> BorrowBooks { get; set; }
}