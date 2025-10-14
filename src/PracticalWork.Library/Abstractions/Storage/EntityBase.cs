namespace PracticalWork.Library.Abstractions.Storage;

/// <summary>
/// Базовый класс для всех сущностей
/// </summary>
public abstract class EntityBase : IEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    protected EntityBase()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}
