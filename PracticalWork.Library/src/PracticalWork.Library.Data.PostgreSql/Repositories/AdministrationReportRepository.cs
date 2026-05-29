using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Data.PostgreSql.Repositories;

public class AdministrationReportRepository: IAdministrationReportRepository
{
    private AppDbContext _context;
    
    public AdministrationReportRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task SaveReportAsync(AdministrationReport report, CancellationToken cancellationToken)
    {
        var entity = new AdministrationReportEntity
        {
            Name = report.Name,
            FilePath = report.FilePath,
            GeneratedAt = report.GeneratedAt,
            Status = report.Status,
            CreatedAt = report.CreatedAt,
        };
        _context.AdministrationReports.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}