using PracticalWork.Library.Contracts.v1.Abstracts;

namespace PracticalWork.Library.Contracts.v1.Books.Request;

/// <summary>
/// Запрос на обновление книги
/// </summary>
public sealed record UpdateBookRequest(string Title, IReadOnlyList<string> Authors, string Description, int Year)
    : AbstractBook(Title, Authors, Description, Year);