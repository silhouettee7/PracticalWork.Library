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
    Task<EmailSendResult> SendAsync(EmailMessage message);
}