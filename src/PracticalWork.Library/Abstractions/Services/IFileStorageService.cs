namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис для управления файлами 
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Загрузить файл
    /// </summary>
    /// <param name="fileName">название файла</param>
    /// <param name="fileStream">поток файла</param>
    /// <param name="contentType">тип файла</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task UploadFileAsync(string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
    /// <summary>
    /// Удалить файл
    /// </summary>
    /// <param name="fileName">название файла</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task DeleteFileAsync(string fileName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Проверить существование файла
    /// </summary>
    /// <param name="fileName">название файла</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>существует или нет</returns>
    Task<bool> FileExistsAsync(string fileName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Получить ссылку на файл
    /// </summary>
    /// <param name="fileName">название файла</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>ссылка</returns>
    Task<string> GetFileLinkAsync(string fileName, CancellationToken cancellationToken = default);
}