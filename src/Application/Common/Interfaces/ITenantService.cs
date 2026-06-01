namespace Application.Common.Interfaces;

/// <summary>
/// Provides access to the current tenant's identity.
/// Resolved by the TenantMiddleware in WebApi and injected into
/// handlers and the DbContext to enforce Row-Level Security.
/// </summary>
public interface ITenantService
{
    Guid TenantId { get; }
}
