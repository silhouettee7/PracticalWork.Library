using Domain.Models;
using PracticalWork.Report.Contracts.v1.Reports.Response;
using PracticalWork.Report.Models;

namespace PracticalWork.Report.Controllers.Mappers.v1;

public static class ActivityLogExtensions
{
    public static ActivityLogResponse ToActivityLogResponse(this ActivityLog activityLog)
    {
        return new ActivityLogResponse(
            activityLog.Event, 
            activityLog.EventType, 
            activityLog.EventDate);
    }

    public static ActivityLogsPaginationResponse ToActivityLogsPaginationResponse(
        this CursorPaginationResponse<ActivityLog> activityLogsCursorPaginationResponse)
    {
        return new ActivityLogsPaginationResponse(
            activityLogsCursorPaginationResponse.Items
                .Select(i => i.ToActivityLogResponse())
                .ToList(),
            activityLogsCursorPaginationResponse.NextCursor,
            activityLogsCursorPaginationResponse.PreviousCursor,
            activityLogsCursorPaginationResponse.HasNext,
            activityLogsCursorPaginationResponse.HasPrevious
            );
    }
}