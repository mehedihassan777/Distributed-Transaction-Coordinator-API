namespace Domain.Common;

/// <summary>
/// Base class for all domain entities. Provides identity and audit fields.
/// Using a base class in the Domain ensures every entity has a consistent
/// identity contract without coupling to any external framework.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    protected void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;
}
