using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Extensions;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Services;

public class NotificationService: INotificationService
{
    private readonly IBorrowRepository _borrowRepository;
    private readonly IEmailService _emailService;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<NotificationService> _logger;
    
    public NotificationService(IBorrowRepository borrowRepository,
        IEmailService emailService,
        OptionsMonitor<EmailOptions> emailOptions,
        ILogger<NotificationService> logger)
    {
        _borrowRepository = borrowRepository;
        _emailService = emailService;
        _emailOptions = emailOptions.CurrentValue;
        _logger = logger;
    }
    public async Task NotifyReadersWithIssuedBorrowedBooksAsync()
    {
        var dateThreeDaysAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var dateOneDayAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var toleranceMinutes = 23 * 60 + 40;
        var dateToleranceMinutesAgo = DateTime.UtcNow.AddMinutes(-toleranceMinutes);
        
        var borrowedBooks = await _borrowRepository
            .GetBorrowedIssuedBooksInfo(dateOneDayAgo, dateThreeDaysAfter, dateToleranceMinutesAgo);
        
        var path = Path.Combine(Directory.GetCurrentDirectory(), "notification_readers.html");
        var emailMessageHtmlBodyTemplate = await File.ReadAllTextAsync(path);
        
        foreach (var borrowBook in borrowedBooks)
        {
            var personalizationObject = borrowBook.ToBorrowedBookNotification();
            var emailTo = _emailOptions.SmtpServer + ":" + _emailOptions.SmtpPort;
            var recipientName = "demo";
            var subject = "Напоминание о возврате книги";
            var emailMessage = new EmailMessage(emailTo, subject, emailMessageHtmlBodyTemplate, true)
                {
                    RecipientName = recipientName
                };
            emailMessage.Personalize(personalizationObject);
            var sentResult = await _emailService.SendAsync(emailMessage);
            if (!sentResult.IsSuccess)
            {
                _logger.LogError("Ошибка отправки письма для {ReaderFullName}", personalizationObject.ReaderFullName);
            }
            else
            {
                _logger.LogInformation("Письмо успешно отправлено для {ReaderFullName}", personalizationObject.ReaderFullName);
            }
            //надо подумать как добиться большей надежности(если БД упадет например или будет ошибка обновления)
            try
            {
                await _borrowRepository.UpdateLastEmailSentAsync(borrowBook.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления даты отправки, возможен повтор уведомления");
            }
        }
    }
}