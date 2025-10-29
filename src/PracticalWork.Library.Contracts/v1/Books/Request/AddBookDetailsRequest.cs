using Microsoft.AspNetCore.Http;

namespace PracticalWork.Library.Contracts.v1.Books.Request;

/// <summary>
/// Запрос на добавления деталей по книге
/// </summary>
/// <param name="Id">Идентификатор книги</param>
/// <param name="Description">Краткое описание книги</param>
/// <param name="CoverImage">Картинка изображения</param>
public sealed record AddBookDetailsRequest(Guid Id, string Description, IFormFile CoverImage);