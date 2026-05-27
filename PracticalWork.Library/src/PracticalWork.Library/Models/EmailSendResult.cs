namespace PracticalWork.Library.Models;

/// <summary>
/// Результат отправки сообщений пользователям
/// </summary>
public sealed class EmailSendResult
{
    public string ResponseMessage { get; set; }
    public bool IsSuccess { get; set; }
}