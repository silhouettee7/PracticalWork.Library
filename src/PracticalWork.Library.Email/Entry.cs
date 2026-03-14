using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Email.Abstractions;
using PracticalWork.Library.Email.Configuration;

namespace PracticalWork.Library.Email;

public static class Entry
{
    public static IServiceCollection AddEmail(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<EmailSettings>(configuration
            .GetSection(EmailSettings.SectionName));
        
        serviceCollection.AddSingleton<ISmtpClient>(s =>
        {
            var options = s.GetService<IOptions<EmailSettings>>()?.Value
                          ?? throw new NullReferenceException("Email settings not found");
            var client = new SmtpClient();
            client.Connect(options.SmtpServer, options.SmtpPort);
            client.Authenticate(options.SenderEmail, options.SenderPassword);
            return client;
        });
        serviceCollection.AddScoped<IEmailService, EmailService>();
        
        return serviceCollection;
    }
}