namespace Domain.Common;

/// <summary>
/// Marker interface for entities that belong to a specific tenant.
/// Every entity implementing this interface will automatically have
/// EF Core Row-Level Security applied via a global query filter in ApplicationDbContext.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; }
}
