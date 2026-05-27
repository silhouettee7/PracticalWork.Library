using Hangfire;
using PracticalWork.Library.Abstractions.Jobs;
using PracticalWork.Library.Abstractions.Services;

namespace PracticalWork.Library.BackgroundTasks.Jobs;

public class ReturnRemindersJob: AbstractLibraryJob
{
    public override string JobName => "Notification";
    public override string Description => "Рассылка напоминаний о возврате книг";
    public override string ConfigSectionName => "NotificationJob";

    public ReturnRemindersJob(IRecurringJobManager jobManager) : base(jobManager)
    {
    }
    
    public override void ExecuteCronJob(string cron)
    {
        _recurringJobManager.AddOrUpdate<INotificationService>(JobName, s => 
            s.NotifyReadersWithIssuedBorrowedBooksAsync(CancellationToken.None), cron);
    }
}