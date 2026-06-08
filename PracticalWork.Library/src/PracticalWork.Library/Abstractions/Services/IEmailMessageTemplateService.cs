namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис для шаблонов письма
/// </summary>
public interface IEmailMessageTemplateService
{
    /// <summary>
    /// Сгенерировать html шаблон письма
    /// </summary>
    /// <param name="fileName">название файла</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>шаблон письма</returns>
    Task<string> GetEmailMessageHtmlBodyTemplateAsync(string fileName,
        CancellationToken cancellationToken);
}