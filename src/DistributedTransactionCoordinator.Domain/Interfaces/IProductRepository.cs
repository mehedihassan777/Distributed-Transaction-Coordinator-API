using DistributedTransactionCoordinator.Domain.Entities;

namespace DistributedTransactionCoordinator.Domain.Interfaces;

/// <summary>
/// Repository abstraction for Product aggregate.
/// Defined in the Domain layer so Application/Domain code can depend on this
/// interface without depending on any infrastructure concern.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
