namespace PracticalWork.Library.Abstractions.Services;

public interface IEmailMessageTemplateService
{
    Task<string> GetEmailMessageHtmlBodyTemplateAsync(string fileName,
        CancellationToken cancellationToken);
}