using Microsoft.EntityFrameworkCore;
using DistributedTransactionCoordinator.Domain.Entities;
using DistributedTransactionCoordinator.Domain.Interfaces;

namespace DistributedTransactionCoordinator.Infrastructure.Persistence;

/// <summary>
/// Application DbContext with built-in Row-Level Security via EF Core global query filters.
/// The tenant filter is applied at context level so no handler can accidentally bypass it.
/// </summary>
public class AppDbContext : DbContext, IUnitOfWork
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Product> Products => Set<Product>();

    // Outbox table for reliable async event publishing
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ── Row-Level Security ──────────────────────────────────────────────────
        // Global query filter ensures every query is automatically scoped to the
        // current tenant. This is the single enforcement point for RLS.
        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events as outbox messages before persisting
        var entitiesWithEvents = ChangeTracker.Entries<Domain.Common.BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        foreach (var entry in entitiesWithEvents)
        {
            foreach (var domainEvent in entry.Entity.DomainEvents)
            {
                OutboxMessages.Add(OutboxMessage.FromDomainEvent(domainEvent));
            }
            entry.Entity.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
