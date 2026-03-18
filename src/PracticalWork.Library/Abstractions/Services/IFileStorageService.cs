namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис для управления файлами 
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Загрузить файл
    /// </summary>
    /// <param name="bucket">название бакета</param>
    /// <param name="fileName">название файла</param>
    /// <param name="fileStream">поток файла</param>
    /// <param name="contentType">тип файла</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task UploadFileAsync(string bucket, string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
    /// <summary>
    /// Получить ссылку на файл
    /// </summary>
    /// <param name="bucket">название бакета</param>
    /// <param name="fileName">название файла</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>ссылка</returns>
    Task<string> GetFileLinkAsync(string bucket, string fileName, CancellationToken cancellationToken = default);
    Task SetBucketFilesLifeTimeAsync(string bucket, DateTime deleteDate, string prefix, CancellationToken cancellationToken = default);
}