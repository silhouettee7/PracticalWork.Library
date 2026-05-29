using Hangfire;
using PracticalWork.Library.Abstractions.Jobs;
using PracticalWork.Library.Abstractions.Services;

namespace PracticalWork.Library.BackgroundTasks.Jobs;

public class WeeklyReportJob: AbstractLibraryJob
{
    public override string JobName => "WeeklyReport";
    public override string Description => "Еженедельный отчет для администрации";
    public override string ConfigSectionName => "WeeklyReportJob";

    public WeeklyReportJob(IRecurringJobManager jobManager) : base(jobManager)
    {

    }
    
    public override void ExecuteCronJob(string cron)
    {
        _recurringJobManager.AddOrUpdate<IAdministrationReportService>(JobName, s => 
            s.CreateReportForAdministration(CancellationToken.None), cron);
    }
}