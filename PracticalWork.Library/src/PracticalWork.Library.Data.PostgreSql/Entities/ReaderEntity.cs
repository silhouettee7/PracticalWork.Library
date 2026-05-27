using PracticalWork.Library.Abstractions.Storage;

namespace PracticalWork.Library.Data.PostgreSql.Entities;

/// <summary>
/// Карточка читателя
/// </summary>
public sealed class ReaderEntity : EntityBase
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

    /// <summary>Записи о взятых книгах</summary>
    public ICollection<BookBorrowEntity> BorrowedRecords { get; set; }
}