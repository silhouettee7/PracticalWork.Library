using PracticalWork.Library.Contracts.v1.Reports.Response;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Controllers.Mappers.v1;

public static class ReportsExtensions
{
    public static ReportResponse ToReportResponse(this Report report)
    {
        return new ReportResponse(
            report.Name, 
            report.FilePath, 
            report.PeriodFrom, 
            report.PeriodTo, 
            report.EventTypes, 
            report.GeneratedAt!.Value);
    }
}