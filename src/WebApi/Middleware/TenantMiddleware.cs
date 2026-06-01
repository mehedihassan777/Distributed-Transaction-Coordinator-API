namespace WebApi.Middleware;

/// <summary>
/// Tenant validation middleware.
///
/// Architecture decision:
/// TenantService reads the tenant_id claim lazily via IHttpContextAccessor.
/// This middleware acts as an early guard — it rejects requests that lack a
/// valid tenant_id claim before they reach the controller layer, providing
/// a clear 401/400 rather than letting a null TenantId propagate silently.
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

    public async Task InvokeAsync(HttpContext context)
    {
        // Only enforce tenant claims for authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst(TenantIdClaimType)?.Value;

            if (string.IsNullOrWhiteSpace(tenantIdClaim))
            {
                _logger.LogWarning("Authenticated request received without a tenant_id claim.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Tenant identifier is missing." });
                return;
            }

            if (!Guid.TryParse(tenantIdClaim, out _))
            {
                _logger.LogWarning("Request received with an invalid tenant_id claim value: {TenantId}", tenantIdClaim);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Tenant identifier is invalid." });
                return;
            }
        }

        await _next(context);
    }
}
