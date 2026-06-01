using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

/// <summary>
/// Design-time factory for ApplicationDbContext.
/// Required for EF Core CLI commands (migrations) which cannot use the DI container.
/// Uses a no-op TenantService since migrations are schema-level and tenant-agnostic.
/// </summary>
public class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=distributed_tx_db;Username=postgres;******";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString,
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeTenantService());
    }

    private sealed class DesignTimeTenantService : ITenantService
    {
        public Guid TenantId => Guid.Empty;
    }
}
