using PracticalWork.Library.Contracts.v1.Abstracts;
using PracticalWork.Library.Contracts.v1.Enums;

namespace PracticalWork.Library.Contracts.v1.Books.Request;

/// <summary>
/// Запрос на создание книги
/// </summary>
/// <param name="Title">Название книги</param>
/// <param name="Category">Категория книги</param>
/// <param name="Authors">Авторы</param>
/// <param name="Description">Краткое описание книги</param>
/// <param name="Year">Год издания</param>
public sealed record CreateBookRequest(string Title, BookCategory Category, IReadOnlyList<string> Authors, string Description, int Year)
    : AbstractBook(Title, Authors, Description, Year);