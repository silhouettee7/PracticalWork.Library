using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Email;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ISmtpClient _smtpClient;
    private readonly ILogger<EmailService> _logger;
    private readonly TimeProvider _timeProvider;

    public EmailService(ISmtpClient client, 
        IOptionsMonitor<EmailOptions> options,
        ILogger<EmailService> logger,
        TimeProvider timeProvider)
    {
        _smtpClient = client;
        _logger = logger;
        _timeProvider = timeProvider;
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
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ResponseMessage = ex.Message;
            result.Exception = ex;
        }

        return result;
    }
    
    public async Task SendWeeklyReportToAdmin(string subject, string adminEmail, BooksStatistic booksStatistic, 
        string htmlTemplate, CancellationToken cancellationToken)
    {
        var email = new EmailMessage(adminEmail, subject, htmlTemplate, true);
        email.Personalize(booksStatistic);
        
        var sentResult = await SendAsync(email, cancellationToken);
        
        if (sentResult.IsSuccess)
        {
            _logger.LogInformation("Отчет успешно отправлен для администратора: {adminEmail}", adminEmail);
        }
        else
        {
            _logger.LogError("Ошибка отправки отчета администратору: {adminEmail}\n\n{ErrorMessage}", adminEmail, 
                sentResult.ResponseMessage);
            throw new EmailServiceException(sentResult.ResponseMessage, sentResult.Exception);
        }
    }
    
    public async Task NotifyReaderAboutBorrowedBookAsync(string emailTo, BorrowedBookNotification borrowedBookNotification, 
        string subject, string emailMessageHtmlBodyTemplate, CancellationToken cancellationToken)
    {
        var emailMessage = GetEmailMessage(emailTo, subject, emailMessageHtmlBodyTemplate, borrowedBookNotification);
        var sentResult = await SendAsync(emailMessage, cancellationToken);
        if (sentResult.IsSuccess)
        {
            _logger.LogInformation("Уведомление отправлено для {emailTo}", emailTo);
        }
        else
        {
            _logger.LogError("Ошибка отправки уведомления для {emalTo}", emailTo);
            throw new EmailServiceException(sentResult.ResponseMessage, sentResult.Exception);
        }
    }
    
    private EmailMessage GetEmailMessage(string emailTo, string subject, 
        string emailMessageHtmlBodyTemplate, 
        BorrowedBookNotification borrowedBookNotification)
    {
        var recipientName = borrowedBookNotification.ReaderFullName;
        var emailMessage = new EmailMessage(emailTo, subject, 
            emailMessageHtmlBodyTemplate, true)
        {
            RecipientName = recipientName
        };
        emailMessage.Personalize(borrowedBookNotification);
        
        return emailMessage;
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