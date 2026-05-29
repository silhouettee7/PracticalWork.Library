namespace PracticalWork.Library.Abstractions.Services;

public interface IAdministrationReportService
{
    Task CreateReportForAdministration(CancellationToken cancellationToken);
}