using Domain.Exceptions;

namespace PracticalWork.Library.Exceptions;

/// <summary>
/// Исключение уровня сервиса отправки писем
/// </summary>
public class EmailServiceException: AppException
{
    public EmailServiceException(string message): base(message)
    {
        
    }

    public EmailServiceException(string message, Exception innerException) : base(message, innerException)
    {
        
    }
}