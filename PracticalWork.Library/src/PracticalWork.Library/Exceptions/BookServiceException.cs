namespace PracticalWork.Library.Exceptions;

/// <summary>
/// Исключения уровня сервиса с книгами
/// </summary>
public sealed class BookServiceException : AppException
{
    public BookServiceException(string message) : base($"{message}")
    {
    }

    public BookServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}