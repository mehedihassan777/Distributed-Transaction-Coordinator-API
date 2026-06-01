using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using DistributedTransactionCoordinator.Application.Common.Interfaces;
using DistributedTransactionCoordinator.Domain.Interfaces;
using DistributedTransactionCoordinator.Infrastructure.Caching;
using DistributedTransactionCoordinator.Infrastructure.Messaging;
using DistributedTransactionCoordinator.Infrastructure.MultiTenancy;
using DistributedTransactionCoordinator.Infrastructure.Persistence;
using DistributedTransactionCoordinator.Infrastructure.Persistence.Repositories;

namespace DistributedTransactionCoordinator.Infrastructure.DependencyInjection;

/// <summary>
/// Extension method to wire up all Infrastructure layer services.
/// Keeps infrastructure concerns (DB, cache, messaging) isolated in this layer.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Multi-Tenancy ───────────────────────────────────────────────────────
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        // ── PostgreSQL / EF Core ────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();

        // ── Redis ───────────────────────────────────────────────────────────────
        services.AddStackExchangeRedisCache(opts =>
        {
            opts.Configuration = configuration.GetConnectionString("Redis");
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        // ── RabbitMQ ────────────────────────────────────────────────────────────
        services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
        {
            Uri = new Uri(configuration.GetConnectionString("RabbitMq")
                         ?? "amqp://localhost:5672/")
        });
        services.AddSingleton<IEventBus, RabbitMqEventBus>();

        return services;
    }
}
