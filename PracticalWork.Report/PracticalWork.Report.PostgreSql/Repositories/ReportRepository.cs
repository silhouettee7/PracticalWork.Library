using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using PracticalWork.Report.Abstractions.Storage;
using PracticalWork.Report.Enums;
using PracticalWork.Report.PostgreSql.Entities;

namespace PracticalWork.Report.PostgreSql.Repositories;

public class ReportRepository: IReportRepository
{
    private readonly ReportsDbContext _context;
    
    public ReportRepository(ReportsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Guid> CreateReport(Models.Report report)
    {
        var entity = new ReportEntity
        {
            PeriodFrom = report.PeriodFrom,
            PeriodTo = report.PeriodTo,
            EventTypes = report.EventTypes,
            Status = report.Status,
        };
        await _context.Reports.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Models.Report>> GetReadyReports()
    {
        var entities = await _context.Reports
            .Where(r => r.Status == ReportStatus.Generated)
            .ToListAsync();
        
        return entities
            .Select(e => new Models.Report
            {
                Name = e.Name,
                EventTypes = e.EventTypes,
                GeneratedAt = e.GeneratedAt,
                PeriodFrom = e.PeriodFrom,
                PeriodTo = e.PeriodTo,
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Models.Report> GetReportById(Guid reportId)
    {
        var reportEntity = await _context.Reports
            .SingleOrDefaultAsync(r => r.Id == reportId)
            ?? throw new EntityNotFoundException($"Не удалось найти отчет с id{reportId}");

        return new Models.Report
        {
            Name = reportEntity.Name,
            EventTypes = reportEntity.EventTypes,
            GeneratedAt = reportEntity.GeneratedAt,
            PeriodFrom = reportEntity.PeriodFrom,
            PeriodTo = reportEntity.PeriodTo,
            Status = reportEntity.Status,
            FilePath = reportEntity.FilePath,
        };
    }

    /// <inheritdoc />
    public async Task<(Guid id, Models.Report report)> GetReportByName(string reportName)
    {
        var reportEntity = await _context.Reports
            .SingleOrDefaultAsync(r => r.Name == reportName) 
            ?? throw new EntityNotFoundException($"Не удалось найти отчет с именем:{reportName}");

        return (reportEntity.Id, new Models.Report
        {
            Name = reportEntity.Name,
            EventTypes = reportEntity.EventTypes,
            GeneratedAt = reportEntity.GeneratedAt,
            PeriodFrom = reportEntity.PeriodFrom,
            PeriodTo = reportEntity.PeriodTo,
            Status = reportEntity.Status,
            FilePath = reportEntity.FilePath,
        });
    }

    /// <inheritdoc />
    public async Task UpdateReport(Guid id, Models.Report report)
    {
        var reportEntity = await _context.Reports
            .SingleOrDefaultAsync(r => r.Id == id)
            ?? throw new EntityNotFoundException("Не удалось найти отчет");
        reportEntity.Name = report.Name;
        reportEntity.EventTypes = report.EventTypes;
        reportEntity.GeneratedAt = report.GeneratedAt;
        reportEntity.PeriodFrom = report.PeriodFrom;
        reportEntity.PeriodTo = report.PeriodTo;
        reportEntity.Status = report.Status;
        reportEntity.FilePath = report.FilePath;
        await _context.SaveChangesAsync();
    }
}