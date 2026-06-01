namespace DistributedTransactionCoordinator.Domain.Interfaces;

/// <summary>
/// Tenant context abstraction — resolved from the JWT token by middleware.
/// Keeping this in Domain prevents any leakage of HttpContext into the domain model.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
}
