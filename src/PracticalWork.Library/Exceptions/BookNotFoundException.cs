namespace PracticalWork.Library.Exceptions;

public class BookNotFoundException: AppException
{
    public BookNotFoundException(string message) : base(message)
    {
        
    }
}