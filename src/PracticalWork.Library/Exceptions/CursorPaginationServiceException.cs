namespace PracticalWork.Library.Exceptions;

public class CursorPaginationServiceException: AppException
{
    public CursorPaginationServiceException(string message, Exception innerException) : base(message, innerException)
    {
        
    }
}