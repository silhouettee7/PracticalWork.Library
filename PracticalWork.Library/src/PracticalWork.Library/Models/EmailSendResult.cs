namespace PracticalWork.Library.Models;

/// <summary>
/// Результат отправки сообщений пользователям
/// </summary>
public sealed class EmailSendResult
{
    /// <summary>
    /// Ответ по отправке
    /// </summary>
    public string ResponseMessage { get; set; }
    /// <summary>
    /// Успешно или нет
    /// </summary>
    public bool IsSuccess { get; set; }
    /// <summary>
    /// Исключение при неуспешной отправке
    /// </summary>
    public Exception Exception { get; set; }
}