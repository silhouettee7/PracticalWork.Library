namespace Domain.Exceptions;

/// <summary>
/// Исключения уровня сервиса работы библиотеки
/// </summary>
public class LibraryServiceException: AppException
{
    public LibraryServiceException(string message) : base(message)
    {
        
    }

    public LibraryServiceException(string message, Exception innerException) : base(message, innerException)
    {
        
    }
}