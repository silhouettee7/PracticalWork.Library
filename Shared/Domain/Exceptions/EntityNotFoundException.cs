namespace Domain.Exceptions;

/// <summary>
/// Исключения уровня бд, если не найдена сущность
/// </summary>
public class EntityNotFoundException: NotFoundException
{
    public EntityNotFoundException(string message): base(message)
    {
        
    }
}