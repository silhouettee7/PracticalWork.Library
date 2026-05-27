using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Email;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ISmtpClient _smtpClient;

    public EmailService(ISmtpClient client, 
        IOptionsMonitor<EmailOptions> options)
    {
        _smtpClient = client;
        _options = options.CurrentValue;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        EmailSendResult result = new();
        try
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
            mimeMessage.To.Add(new MailboxAddress(message.RecipientName, message.EmailTo));
            mimeMessage.Subject = message.Subject;
            mimeMessage.Body = GetBodyBuilder(message).ToMessageBody();

            var response = await _smtpClient.SendAsync(mimeMessage, cancellationToken);
            result.IsSuccess = true;
            result.ResponseMessage = response;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ResponseMessage = ex.Message;
        }

        return result;
    }

    private BodyBuilder GetBodyBuilder(EmailMessage message)
    {
        var builder = new BodyBuilder();
        if (message.IsBodyHtml)
        {
            builder.HtmlBody = message.Body;
        }
        else
        {
            builder.TextBody = message.Body;
        }
        return builder;
    }
}