namespace PracticalWork.Library.Abstractions.Jobs;

/// <summary>
/// Абстракция для фоновой задачи
/// </summary>
public interface ILibraryJob
{
    /// <summary>
    /// Название задачи
    /// </summary>
    string JobName { get; }
    /// <summary>
    /// Описание задачи
    /// </summary>
    string Description { get; }
    /// <summary>
    /// Секция в конфиг файле для настройки
    /// </summary>
    string ConfigSectionName { get; }
    /// <summary>
    /// Стартануть джобу
    /// </summary>
    /// <param name="cron">время выполнения в формате крон</param>
    void ExecuteCronJob(string cron);
    /// <summary>
    /// Удалить джобу
    /// </summary>
    /// <param name="jobName"></param>
    void DeleteJob(string jobName);
}