using PracticalWork.Report.Contracts.v1.Reports.Response;

namespace PracticalWork.Report.Controllers.Mappers.v1;

public static class ReportsExtensions
{
    public static ReportResponse ToReportResponse(this Models.Report report)
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