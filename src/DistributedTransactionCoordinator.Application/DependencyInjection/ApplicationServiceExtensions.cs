using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;
using DistributedTransactionCoordinator.Application.Common.Behaviours;

namespace DistributedTransactionCoordinator.Application.DependencyInjection;

/// <summary>
/// Extension method to register all Application layer services.
/// Each layer owns its own DI registration to maintain separation of concerns.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));

        return services;
    }
}
