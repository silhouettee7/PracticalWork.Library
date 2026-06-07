using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

public interface IReportGenerateService
{
    ReportGenerateResult GenerateReport<T>(IEnumerable<T> items, string fileName);
    ReportGenerateResult GenerateReportForAdministration(string reportName, BooksStatistic booksStatistic);
    ReportGenerateResult GenerateArchiveReport(string reportName, ArchiveLog archiveLog);
}