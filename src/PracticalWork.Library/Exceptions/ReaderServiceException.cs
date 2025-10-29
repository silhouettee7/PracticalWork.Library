namespace PracticalWork.Library.Exceptions;

public class ReaderServiceException: Exception
{
    public ReaderServiceException(string message) : base(message)
    {
    }
    public ReaderServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}