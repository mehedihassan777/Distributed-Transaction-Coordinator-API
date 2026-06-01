using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

/// <summary>
/// Resolves the current tenant from the HTTP context.
/// The TenantMiddleware validates the JWT claim before this service is called.
/// Using IHttpContextAccessor makes the service safely injectable in any scoped service,
/// and the tenant_id is read lazily — each access resolves fresh from the current request.
/// </summary>
public class TenantService : ITenantService
{
    private const string TenantIdClaimType = "tenant_id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?
                .User.FindFirst(TenantIdClaimType)?.Value;

            if (string.IsNullOrWhiteSpace(tenantIdClaim) ||
                !Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                // Return Empty as a safe default for design-time / background services.
                // The TenantMiddleware blocks unauthenticated requests before handlers run.
                return Guid.Empty;
            }

            return tenantId;
        }
    }
}
