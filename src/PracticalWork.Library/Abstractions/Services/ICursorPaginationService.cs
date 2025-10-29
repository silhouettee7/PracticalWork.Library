using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

public interface ICursorPaginationService<TModel> where TModel : ICursor
{
    CursorPaginationResponse<TModel> ToCursorPageResponse(IReadOnlyList<TModel> page, CursorPaginationRequest request);
}