using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Dtos;
using PracticalWork.Library.Extensions;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Application.Services;

public class NotificationService: INotificationService
{
    private readonly IBorrowRepository _borrowRepository;
    private readonly IEmailService _emailService;
    private readonly EmailOptions _emailOptions;
    private readonly EmailMessagesOptions _emailMessagesOptions;
    private readonly SchedulerOptions _schedulerOptions;
    private readonly ILogger<NotificationService> _logger;
    private readonly TimeProvider _timeProvider;
    
    public NotificationService(IBorrowRepository borrowRepository,
        IEmailService emailService,
        IOptionsMonitor<EmailOptions> emailOptions,
        ILogger<NotificationService> logger, 
        TimeProvider timeProvider, 
        IOptionsMonitor<EmailMessagesOptions> emailMessagesOptions, 
        IOptionsMonitor<SchedulerOptions> schedulerOptions)
    {
        _borrowRepository = borrowRepository;
        _emailService = emailService;
        _emailOptions = emailOptions.CurrentValue;
        _logger = logger;
        _timeProvider = timeProvider;
        _schedulerOptions = schedulerOptions.CurrentValue;
        _emailMessagesOptions = emailMessagesOptions.CurrentValue;
    }
    
    public async Task NotifyReadersWithIssuedBorrowedBooksAsync(CancellationToken cancellationToken)
    {
        var borrowedBooks = await GetBorrowedIssuedBooksInfoAsync(cancellationToken);
        var emailMessageHtmlBodyTemplate = await GetEmailMessageHtmlBodyTemplateAsync(cancellationToken);
        
        foreach (var borrowBook in borrowedBooks)
        {
            await NotifyReaderAboutBorrowedBookAsync(borrowBook, 
                emailMessageHtmlBodyTemplate,cancellationToken);
        }
    }
    
    private async Task<List<BorrowedIssuedBookInfoDto>> GetBorrowedIssuedBooksInfoAsync(CancellationToken cancellationToken)
    {
        var dateThreeDaysAfter = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime.AddDays(3));
        var dateOneDayAgo = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime.AddDays(-1));
        var toleranceMinutes = _schedulerOptions.NotificationToleranceMinutes;
        var dateToleranceMinutesAgo = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-toleranceMinutes);
        
        return await _borrowRepository.GetBorrowedIssuedBooksInfo(
            dateOneDayAgo, dateThreeDaysAfter, dateToleranceMinutesAgo, cancellationToken);
    }

    private async Task<string> GetEmailMessageHtmlBodyTemplateAsync(CancellationToken cancellationToken)
    {
        var fileName = _emailMessagesOptions.Notification.TemplateFileName; 
        var path = Path.Combine(Directory.GetCurrentDirectory(),  fileName );
        
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    private async Task NotifyReaderAboutBorrowedBookAsync(BorrowedIssuedBookInfoDto borrowBook, 
        string emailMessageHtmlBodyTemplate, CancellationToken cancellationToken)
    {
        var borrowedBookNotification = borrowBook.ToBorrowedBookNotification(_timeProvider);
        var emailMessage = GetEmailMessage(emailMessageHtmlBodyTemplate, borrowedBookNotification);
        var sentResult = await _emailService.SendAsync(emailMessage, cancellationToken);
        if (sentResult.IsSuccess)
        {
            _logger.LogInformation("Уведомление отправлено для {ReaderFullName}", borrowedBookNotification.ReaderFullName);
            await UpdateLastEmailSentAsync(borrowBook.Id, cancellationToken);
        }
        else
        {
            _logger.LogError("Ошибка отправки уведомления для {ReaderFullName}", borrowedBookNotification.ReaderFullName);
        }
    }
    
    private EmailMessage GetEmailMessage(string emailMessageHtmlBodyTemplate, 
        BorrowedBookNotification borrowedBookNotification)
    {
        // в дальнейшем заменить на почту настоящих пользователей из БД
        var shortGuid = Guid.NewGuid().ToString()[..8];
        var emailTo = $"{shortGuid}@test.com";
        var recipientName = borrowedBookNotification.ReaderFullName;
        var subject = _emailMessagesOptions.Notification.Subject;
        var emailMessage = new EmailMessage(emailTo, subject, 
            emailMessageHtmlBodyTemplate, true)
        {
            RecipientName = recipientName
        };
        emailMessage.Personalize(borrowedBookNotification);
        
        return emailMessage;
    }

    private async Task UpdateLastEmailSentAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _borrowRepository.UpdateLastEmailSentAsync(
                id, 
                _timeProvider.GetUtcNow().UtcDateTime, 
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обновления даты отправки, возможен повтор уведомления");
            throw;
        }
    }
}