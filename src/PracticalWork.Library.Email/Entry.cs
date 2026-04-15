using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Email;

public static class Entry
{
    public static IServiceCollection AddEmail(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<EmailOptions>(configuration
            .GetSection("App:Email"));
        
        serviceCollection.AddScoped<ISmtpClient>(s =>
        {
            var options = s.GetService<IOptions<EmailOptions>>()?.Value
                          ?? throw new NullReferenceException("Email settings not found");
            var client = new SmtpClient();
            client.Connect(options.SmtpServer, options.SmtpPort);
            client.AuthenticationMechanisms.Clear(); //временно отключаем аутх для почтового сервера
            return client;
        });
        serviceCollection.AddScoped<IEmailService, EmailService>();
        
        return serviceCollection;
    }
}