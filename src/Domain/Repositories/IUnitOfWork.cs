namespace Domain.Repositories;

/// <summary>
/// Unit of Work pattern for coordinating multiple repository operations in a single transaction.
/// Abstracting the transaction boundary in Domain keeps EF Core specifics out of Application logic.
/// </summary>
public interface IUnitOfWork
{
    IProductRepository Products { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
