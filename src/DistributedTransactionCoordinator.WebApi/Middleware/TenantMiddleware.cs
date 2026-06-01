using System.Security.Claims;
using DistributedTransactionCoordinator.Infrastructure.MultiTenancy;

namespace DistributedTransactionCoordinator.WebApi.Middleware;

/// <summary>
/// Tenant resolution middleware.
/// Reads the "tenant_id" claim from the validated JWT and populates the scoped TenantContext.
/// Must be placed AFTER UseAuthentication() so the JWT is already validated.
/// </summary>
public class TenantMiddleware
{
    private const string TenantIdClaimType = "tenant_id";
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var tenantClaim = context.User.FindFirst(TenantIdClaimType)?.Value;

        if (!string.IsNullOrWhiteSpace(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
        {
            tenantContext.SetTenantId(tenantId);
            _logger.LogDebug("Tenant context set to {TenantId}", tenantId);
        }
        else
        {
            _logger.LogWarning("No valid tenant_id claim found. Request may fail if tenant-scoped resources are accessed.");
        }

        await _next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<TenantMiddleware>();
}
