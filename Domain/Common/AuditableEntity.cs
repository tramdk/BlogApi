namespace BlogApi.Domain.Common;

/// <summary>
/// Entity with auditing fields (CreatedAt, UpdatedAt).
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Entity with auditing fields and user tracking.
/// </summary>
public abstract class AuditableEntityWithUser : AuditableEntity
{
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
