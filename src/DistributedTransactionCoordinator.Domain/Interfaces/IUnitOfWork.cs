namespace DistributedTransactionCoordinator.Domain.Interfaces;

/// <summary>
/// Unit of Work abstraction for coordinating multiple repositories in a single transaction.
/// Lives in Domain so Application handlers can commit atomically without EF Core leakage.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
