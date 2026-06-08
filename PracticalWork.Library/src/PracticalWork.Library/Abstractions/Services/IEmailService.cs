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
    /// <summary>
    /// Отправить еженедельный отчет администрации
    /// </summary>
    /// <param name="subject">тема</param>
    /// <param name="adminEmail">почта</param>
    /// <param name="booksStatistic">данные</param>
    /// <param name="htmlTemplate">шаблон</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task SendWeeklyReportToAdmin(string subject, string adminEmail, BooksStatistic booksStatistic,
        string htmlTemplate, CancellationToken cancellationToken);
    /// <summary>
    /// Отправить письмо читателю о выдачах
    /// </summary>
    /// <param name="emailTo">почта</param>
    /// <param name="borrowedBookNotification">данные</param>
    /// <param name="subject">тема</param>
    /// <param name="emailMessageHtmlBodyTemplate">шаблон</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task NotifyReaderAboutBorrowedBookAsync(string emailTo, BorrowedBookNotification borrowedBookNotification,
        string subject, string emailMessageHtmlBodyTemplate, CancellationToken cancellationToken);
}