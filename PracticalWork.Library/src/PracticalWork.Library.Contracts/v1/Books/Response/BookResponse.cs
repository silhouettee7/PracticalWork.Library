using PracticalWork.Library.Contracts.v1.Abstracts;
using PracticalWork.Library.Contracts.v1.Enums;

namespace PracticalWork.Library.Contracts.v1.Books.Response;

/// <summary>
/// Ответ с информацией по книге
/// </summary>
/// <param name="Title">Название книги</param>
/// <param name="Category">Категория книги</param>
/// <param name="Authors">Авторы</param>
/// <param name="Description">Краткое описание книги</param>
/// <param name="Year">Год издания</param>
/// <param name="Status">Статус</param>
/// <param name="IsArchived">В архиве</param>
public sealed record BookResponse(
    string Title,
    BookCategory Category,
    IReadOnlyList<string> Authors,
    string Description,
    int Year,
    BookStatus Status,
    bool IsArchived
) : AbstractBook(Title, Authors, Description, Year);