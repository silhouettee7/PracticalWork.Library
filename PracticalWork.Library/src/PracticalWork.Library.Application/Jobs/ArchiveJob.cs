using Hangfire;
using PracticalWork.Library.Abstractions.Services;

namespace PracticalWork.Library.Application.Jobs;

public class ArchiveJob: AbstractLibraryJob
{
    public override string JobName => "Archive";
    public override string Description => "Архивирование старых книг";
    public override string ConfigSectionName => "ArchiveJob";

    public ArchiveJob(IRecurringJobManager jobManager): base(jobManager)
    {
    }
    
    public override void ExecuteCronJob(string cron)
    {
        _recurringJobManager.AddOrUpdate<IArchiveService>(JobName, s => 
            s.ArchiveOldBooksAsync(CancellationToken.None), cron);
    }
}