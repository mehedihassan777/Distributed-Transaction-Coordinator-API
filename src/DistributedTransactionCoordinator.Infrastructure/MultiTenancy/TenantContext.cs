using DistributedTransactionCoordinator.Domain.Interfaces;

namespace DistributedTransactionCoordinator.Infrastructure.MultiTenancy;

/// <summary>
/// Scoped implementation of ITenantContext.
/// Set by TenantMiddleware at the start of each request after JWT validation.
/// Scoped lifetime ensures each request has its own isolated tenant value.
/// </summary>
public class TenantContext : ITenantContext
{
    private Guid _tenantId;

    public Guid TenantId
    {
        get
        {
            if (_tenantId == Guid.Empty)
                throw new InvalidOperationException(
                    "Tenant context has not been initialised. Ensure TenantMiddleware is registered.");
            return _tenantId;
        }
    }

    public void SetTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        _tenantId = tenantId;
    }
}
