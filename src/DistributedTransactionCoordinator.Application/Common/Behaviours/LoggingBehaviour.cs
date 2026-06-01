using MediatR;
using Microsoft.Extensions.Logging;

namespace DistributedTransactionCoordinator.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that logs command/query execution time.
/// Cross-cutting concerns like logging belong in a pipeline, not in handlers.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next();

        logger.LogInformation("Handled {RequestName}", requestName);
        return response;
    }
}
