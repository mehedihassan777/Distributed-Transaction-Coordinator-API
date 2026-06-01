using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// Repository contract for Product persistence.
/// Defined in the Domain layer so the Application layer can depend on
/// the abstraction without knowing about EF Core or PostgreSQL.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Update(Product product);
    void Remove(Product product);
}
