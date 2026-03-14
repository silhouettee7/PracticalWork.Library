using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using PracticalWork.Library.Email.Abstractions;
using PracticalWork.Library.Email.Configuration;
using PracticalWork.Library.Email.Models;

namespace PracticalWork.Library.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _options;
    private readonly ISmtpClient _smtpClient;

    public EmailService(ISmtpClient client, 
        OptionsMonitor<EmailSettings> options)
    {
        _smtpClient = client;
        _options = options.CurrentValue;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
        mimeMessage.To.Add(new MailboxAddress(null,message.EmailTo));
        mimeMessage.Subject = message.Subject;
        if (message.IsHtml)
        {
            var builder = new BodyBuilder
            {
                HtmlBody = message.Body
            };
            mimeMessage.Body = builder.ToMessageBody();
        }

        EmailSendResult result = new();
        try
        {
            var response = await _smtpClient.SendAsync(mimeMessage);
            result.IsSuccess = true;
            result.ResponseMessage = response;
        }
        catch (Exception)
        {
            result.IsSuccess = false;
        }

        return result;
    }
}