using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Library.Abstractions.Jobs;
using PracticalWork.Library.Abstractions.Services;

namespace PracticalWork.Library.Controllers.Api.v1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/jobs")]
public class JobController: Controller
{
    [HttpPost("/restart/{jobName}/{cron}")]
    public IActionResult RestartCronJob(string jobName, string cron, 
        IServiceProvider serviceProvider)
    {
        var job = serviceProvider.GetServices<ILibraryJob>()
            .SingleOrDefault(j => j.JobName == jobName);
        if (job is null)
        {
            return BadRequest("Job not found");
        }
        job.DeleteJob(jobName);
        job.ExecuteCronJob(cron);

        return Ok();
    }
    
    [HttpPost("/notify")]
    public IActionResult Notify(IServiceProvider serviceProvider)
    {
        var scope = serviceProvider.CreateScope();
        var job = scope.ServiceProvider.GetService<INotificationService>();
        job.NotifyReadersWithIssuedBorrowedBooksAsync(CancellationToken.None);

        return Ok();
    }
}