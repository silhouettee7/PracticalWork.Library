using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис для отправки email сообщений в системе
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Отправить email
    /// </summary>
    /// <param name="message">
    /// <see cref="EmailMessage"/>, содержит информацию о письме:
    /// получателя, тему, тело (HTML и текст), дополнительные параметры
    /// </param>
    /// <param name="cancellationToken">токен отмены</param>
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken);

    Task SendWeeklyReportToAdmin(string subject, string adminEmail, BooksStatistic booksStatistic,
        string htmlTemplate, CancellationToken cancellationToken);

    Task NotifyReaderAboutBorrowedBookAsync(string emailTo, BorrowedBookNotification borrowedBookNotification,
        string subject, string emailMessageHtmlBodyTemplate, CancellationToken cancellationToken);
}