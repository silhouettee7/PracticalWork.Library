using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис для работы с пагинацией
/// </summary>
/// <typeparam name="TModel">тип объекта пагинации</typeparam>
public interface ICursorPaginationService<TModel> where TModel : ICursor
{
    /// <summary>
    /// преобразовать список объектов в рамках страницы в ответ пагинации
    /// </summary>
    /// <param name="page">список объектов</param>
    /// <param name="request">запрос пагинации</param>
    /// <returns>ответ пагинации</returns>
    CursorPaginationResponse<TModel> ToCursorPageResponse(IReadOnlyList<TModel> page, CursorPaginationRequest request);
}