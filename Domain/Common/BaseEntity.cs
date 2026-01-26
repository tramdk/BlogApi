namespace BlogApi.Domain.Common;

/// <summary>
/// Base entity with common properties for all entities.
/// </summary>
/// <typeparam name="TKey">Type of the primary key</typeparam>
public abstract class BaseEntity<TKey>
{
    public TKey Id { get; set; } = default!;
}

/// <summary>
/// Base entity with Guid primary key.
/// </summary>
public abstract class BaseEntity : BaseEntity<Guid>
{
}
