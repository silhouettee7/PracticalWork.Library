using Hangfire;
using PracticalWork.Library.Abstractions.Jobs;

namespace PracticalWork.Library.BackgroundTasks.Jobs;

public abstract class AbstractLibraryJob: ILibraryJob
{
    protected IRecurringJobManager _recurringJobManager;

    protected AbstractLibraryJob(IRecurringJobManager recurringJobManager)
    {
        _recurringJobManager = recurringJobManager;
    }

    public abstract string JobName { get; }
    public abstract string Description { get; }
    public abstract string ConfigSectionName { get; }
    public abstract void ExecuteCronJob(string cron);

    public void DeleteJob(string jobName)
    {
        _recurringJobManager.RemoveIfExists(jobName);
    }
}