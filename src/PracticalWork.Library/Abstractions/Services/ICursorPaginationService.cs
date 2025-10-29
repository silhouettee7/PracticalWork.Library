using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

public interface ICursorPaginationService<TModel>
{
    CursorPaginationResponse<TModel> ToCursorPageResponse(IReadOnlyList<TModel> page, CursorPaginationRequest request);
}