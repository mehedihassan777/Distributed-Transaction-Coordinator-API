using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// Core EF Core DbContext with Row-Level Security via Global Query Filters.
///
/// Architecture decision:
/// - ITenantService is injected to resolve the current TenantId at runtime.
/// - HasQueryFilter on each ITenantEntity automatically appends
///   WHERE TenantId = @currentTenantId to every query, preventing data
///   from leaking between tenants without any per-query boilerplate.
/// - The filter uses a lambda that captures _tenantService.TenantId so it
///   is evaluated lazily on each request, not baked in at startup.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly ITenantService _tenantService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantService tenantService) : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations from the same assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Apply Row-Level Security (RLS) to every entity that implements ITenantEntity.
        // This is the heart of multi-tenancy — the filter runs for ALL queries on
        // any tenant-scoped entity, including eager-loaded navigations.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(BuildTenantFilter(entityType.ClrType));
            }
        }
    }

    private System.Linq.Expressions.LambdaExpression BuildTenantFilter(Type entityType)
    {
        // Builds: entity => entity.TenantId == _tenantService.TenantId
        var param = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var tenantIdProperty = System.Linq.Expressions.Expression.Property(param, nameof(ITenantEntity.TenantId));
        var tenantServiceField = System.Linq.Expressions.Expression.Constant(_tenantService);
        var tenantIdValue = System.Linq.Expressions.Expression.Property(tenantServiceField, nameof(ITenantService.TenantId));
        var body = System.Linq.Expressions.Expression.Equal(tenantIdProperty, tenantIdValue);
        return System.Linq.Expressions.Expression.Lambda(body, param);
    }
}
