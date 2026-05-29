using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

public interface IReportGenerateService
{
    ReportGenerateResult GenerateReport<T>(IEnumerable<T> items, string fileName);
}