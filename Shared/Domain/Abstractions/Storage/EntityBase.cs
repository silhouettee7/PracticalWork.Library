namespace Domain.Abstractions.Storage;

/// <summary>
/// Базовый класс для всех сущностей
/// </summary>
public abstract class EntityBase : IEntity
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTime? UpdatedAt { get; set; }

    protected EntityBase()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}
