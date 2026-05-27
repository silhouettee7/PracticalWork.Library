namespace PracticalWork.Library.Abstractions.Jobs;

public interface ILibraryJob
{
    string JobName { get; }
    string Description { get; }
    string ConfigSectionName { get; }
    void ExecuteCronJob(string cron);
    
    void DeleteJob(string jobName);
}