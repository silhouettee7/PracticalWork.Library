using PracticalWork.Library.Abstractions.Services;

namespace PracticalWork.Library.Application.Services;

public class EmailMessageTemplateService: IEmailMessageTemplateService
{
    public async Task<string> GetEmailMessageHtmlBodyTemplateAsync(string fileName, 
        CancellationToken cancellationToken)
    {
        var htmlTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), fileName); 
        
        return await File.ReadAllTextAsync(htmlTemplatePath, cancellationToken);
    }
}