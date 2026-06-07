using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Dtos;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Extensions;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Application.Services;

public class NotificationService: INotificationService
{
    private readonly IBorrowRepository _borrowRepository;
    private readonly IEmailService _emailService;
    private readonly EmailMessagesOptions _emailMessagesOptions;
    private readonly SchedulerOptions _schedulerOptions;
    private readonly ILogger<NotificationService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IEmailMessageTemplateService _emailMessageTemplateService;

    public NotificationService(IBorrowRepository borrowRepository,
        IEmailService emailService,
        ILogger<NotificationService> logger, 
        TimeProvider timeProvider, 
        IEmailMessageTemplateService emailMessageTemplateService,
        IOptionsMonitor<EmailMessagesOptions> emailMessagesOptions, 
        IOptionsMonitor<SchedulerOptions> schedulerOptions)
    {
        _borrowRepository = borrowRepository;
        _emailService = emailService;
        _logger = logger;
        _timeProvider = timeProvider;
        _emailMessageTemplateService = emailMessageTemplateService;
        _schedulerOptions = schedulerOptions.CurrentValue;
        _emailMessagesOptions = emailMessagesOptions.CurrentValue;
    }
    
    public async Task NotifyReadersWithIssuedBorrowedBooksAsync(CancellationToken cancellationToken)
    {
        var borrowedBooks = await GetBorrowedIssuedBooksInfoAsync(cancellationToken);
        var emailMessageHtmlBodyTemplate = await _emailMessageTemplateService.GetEmailMessageHtmlBodyTemplateAsync(
            _emailMessagesOptions.Notification.TemplateFileName, cancellationToken);;
            
        foreach (var borrowBook in borrowedBooks)
        {
            try
            {
                await _emailService.NotifyReaderAboutBorrowedBookAsync(
                    borrowBook.ReaderFullName,
                    borrowBook.ToBorrowedBookNotification(_timeProvider),
                    _emailMessagesOptions.Notification.Subject,
                    emailMessageHtmlBodyTemplate, cancellationToken);
            }
            catch (EmailServiceException)
            {
                _logger.LogError("Читатель {adminEmail} не получил письма. Отправка не прерывается",
                    borrowBook.ReaderFullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла неизвестная ошибка при отправке сообщения. Отправка не прерывается");
            }
            await UpdateLastEmailSentAsync(borrowBook.Id, cancellationToken);
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
            _logger.LogError(ex, "Письмо отправилось, но обновление даты отправки не произошло. Идентификатор выдачи:{id}." +
                                 " Возможен повтор уведомления при перезапуске задачи (если письмо было успешно отправлено)", id);
        }
    }
}