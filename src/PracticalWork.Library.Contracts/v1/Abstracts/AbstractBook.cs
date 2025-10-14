namespace PracticalWork.Library.Contracts.v1.Abstracts;

/// <param name="Title">Название книги</param>
/// <param name="Authors">Авторы</param>
/// <param name="Description">Краткое описание книги</param>
/// <param name="Year">Год издания</param>
public abstract record AbstractBook(string Title, IReadOnlyList<string> Authors, string Description, int Year);