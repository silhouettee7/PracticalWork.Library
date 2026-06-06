using Domain.Models;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Exceptions;

namespace PracticalWork.Library.Models;

/// <summary>
/// Книга
/// </summary>
public sealed class Book: ICursor
{
    /// <summary>
    /// Идентификатор для курсорной пагинации
    /// </summary>
    public Cursor Cursor { get; set; }
    /// <summary>Название книги</summary>
    public string Title { get; set; }

    /// <summary>Авторы</summary>
    public IReadOnlyList<string> Authors { get; set; }

    /// <summary>Краткое описание книги</summary>
    public string Description { get; set; }

    /// <summary>Год издания</summary>
    public int Year { get; set; }

    /// <summary>Категория</summary>
    public BookCategory Category { get; set; }

    /// <summary>Статус</summary>
    public BookStatus Status { get; set; }

    /// <summary>Путь к изображению обложки</summary>
    public string CoverImagePath { get; set; }

    /// <summary>В архиве</summary>
    public bool IsArchived { get; set; }
    /// <summary>
    /// Записи о выдачах книги
    /// </summary>
    public IReadOnlyList<BookBorrow> IssuanceRecords  { get; set; }

    /// <summary>Проверка перевода в архив</summary>
    public bool CanBeArchived() => Status != BookStatus.Borrow;

    /// <summary>Проверка выдачи на руки</summary>
    public bool CanBeBorrowed() => !IsArchived && Status == BookStatus.Available;

    /// <summary>Перевод в архив</summary>
    public void Archive()
    {
        if (!CanBeArchived())
            throw new BookServiceException("Книга не может быть заархивирована. Она выдана читателю");
        if (Status == BookStatus.Archived || IsArchived)
        {
            throw new BookServiceException("Попытка повторной архивации книги");
        }
        IsArchived = true;
        Status = BookStatus.Archived;
    }
    
    /// <summary>
    /// Обновление информации о книге
    /// </summary>
    /// <param name="title">название книги</param>
    /// <param name="description">описание</param>
    /// <param name="year">год</param>
    /// <param name="authors">авторы</param>
    public void Update(string title, string description, int year, IReadOnlyList<string> authors)
    {
        Title = title;
        Description = description;
        Year = year;
        Authors = authors;
    }
}