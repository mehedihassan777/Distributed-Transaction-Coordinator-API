using Application.Common.Interfaces;
using Domain.Repositories;
using Infrastructure.Caching;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Infrastructure;

/// <summary>
/// Extension method to register all Infrastructure layer services.
/// Infrastructure wires up EF Core, Redis, and RabbitMQ without leaking
/// implementation details to the WebApi layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL via EF Core
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Repositories & Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // Tenant service — resolved per-request from the JWT claim via IHttpContextAccessor
        // Note: AddHttpContextAccessor() must be called in the WebApi layer (Program.cs)
        services.AddScoped<ITenantService, TenantService>();

        // Redis distributed cache
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var connectionString = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Redis connection string 'Redis' is not configured.");
            return ConnectionMultiplexer.Connect(connectionString);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // RabbitMQ event publisher
        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var host = configuration["RabbitMQ:Host"] ?? "localhost";
            var port = int.TryParse(configuration["RabbitMQ:Port"], out var p) ? p : 5672;
            var username = configuration["RabbitMQ:Username"] ?? "guest";
            var password = configuration["RabbitMQ:Password"] ?? "guest";
            return new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = username,
                Password = password,
                AutomaticRecoveryEnabled = true
            };
        });
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        return services;
    }
}
