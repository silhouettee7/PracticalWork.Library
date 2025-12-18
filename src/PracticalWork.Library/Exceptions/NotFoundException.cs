namespace PracticalWork.Library.Exceptions;

/// <summary>
/// Исключение, если объект не найден
/// </summary>
public class NotFoundException: AppException
{
    public NotFoundException(string message): base(message)
    {
        
    }
}