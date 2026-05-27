using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Services;

public class ActivityLogService: IActivityLogService
{
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly ICursorPaginationService<ActivityLog> _cursorPaginationService;

    public ActivityLogService(IActivityLogRepository activityLogRepository,
        ICursorPaginationService<ActivityLog> cursorPaginationService)
    {
        _activityLogRepository = activityLogRepository;
        _cursorPaginationService = cursorPaginationService;
    }

    public async Task<CursorPaginationResponse<ActivityLog>> ReadSystemActivityLogs(CursorPaginationRequest request, 
        string[] eventTypes, DateOnly? eventDateFrom, DateOnly? eventDateTo)
    {
        var logs = await _activityLogRepository
            .GetLogsPageAsync(request, eventDateFrom, eventDateTo, eventTypes);
        return _cursorPaginationService.ToCursorPageResponse(logs, request);
    }
}