namespace PracticalWork.Library.Exceptions;

public class ReaderServiceException: AppException
{
    public ReaderServiceException(string message) : base(message)
    {
    }
    public ReaderServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}