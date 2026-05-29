using Asp.Versioning;
using Contracts.Mappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PracticalWork.Report.Abstractions.Services;
using PracticalWork.Report.Contracts.v1.Reports.Request;
using PracticalWork.Report.Controllers.Mappers.v1;

namespace PracticalWork.Report.Controllers.Api.v1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/reports")]
public class ReportController: Controller
{
    private readonly IReportService _reportService;
    private readonly IActivityLogService _activityLogService;

    public ReportController(IReportService reportService,
        IActivityLogService activityLogService)
    {
        _reportService = reportService;
        _activityLogService = activityLogService;
    }
    
    /// <summary>
    /// Получение страницы записей о событиях системы
    /// </summary>
    /// <param name="request">объект пагинации</param>
    /// <returns>страница с записями</returns>
    [HttpPost("/activity")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetActivityLogs(ActivityLogsPaginationRequest request)
    {
        var result = await _activityLogService.ReadSystemActivityLogs(
            request.ToCursorPaginationRequest(), 
            request.EventTypes.ToArray(),
            request.PeriodFrom, request.PeriodTo);
        return Ok(result.ToActivityLogsPaginationResponse());
    }
    
    /// <summary>
    /// Создать отчет csv
    /// </summary>
    /// <param name="request">отчет с фильтрами</param>
    /// <returns>created</returns>
    [HttpPost("/generate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateReportCsv(ReportCreateRequest request)
    {
        await _reportService.CreateReport(
            request.PeriodFrom, 
            request.PeriodTo, 
            request.EventTypes.ToArray());
        return Created();
    }
    
    /// <summary>
    /// Получить созданные отчеты
    /// </summary>
    /// <returns>информация об отчетах</returns>
    [HttpGet("/")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGeneratedReports()
    {
        var result = await _reportService.GetListOfReadyReports();
        return Ok(result.Select(r => r.ToReportResponse()));
    }
    
    /// <summary>
    /// Получение ссылки на файл отчета
    /// </summary>
    /// <param name="reportName">название файла отчета</param>
    /// <returns>url отчета</returns>
    [HttpGet("/{reportName}/download")]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGeneratedReportUrl(string reportName)
    {
        var result = await _reportService.GetReportUrl(reportName);
        return Ok(result);
    }
}