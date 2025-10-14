using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;

namespace PracticalWork.Library.Data.PostgreSql.Entities;

/// <summary>
/// Базовый класс для книг
/// </summary>
public abstract class AbstractBookEntity : EntityBase
{
    /// <summary>Название книги</summary>
    public string Title { get; set; }

    /// <summary>Авторы</summary>
    public IReadOnlyList<string> Authors { get; set; }

    /// <summary>Краткое описание книги</summary>
    public string Description { get; set; }

    /// <summary>Год издания</summary>
    public int Year { get; set; }

    /// <summary>Статус</summary>
    public BookStatus Status { get; set; }

    /// <summary>Путь к изображению обложки</summary>
    public string CoverImagePath { get; set; }

    /// <summary>Записи о выдачи книги</summary>
    public ICollection<BookBorrowEntity> IssuanceRecords { get; set; }
}