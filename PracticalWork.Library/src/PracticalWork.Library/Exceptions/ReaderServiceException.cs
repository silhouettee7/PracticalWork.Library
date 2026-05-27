namespace PracticalWork.Library.Exceptions;

/// <summary>
/// Исключение уровня сервиса с карточками читателя
/// </summary>
public class ReaderServiceException: AppException
{
    public ReaderServiceException(string message) : base(message)
    {
    }
    public ReaderServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}