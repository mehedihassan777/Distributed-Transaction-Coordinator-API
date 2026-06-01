using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IProductRepository.
/// Lives in Infrastructure — the only layer that references EF Core.
/// The global query filter in ApplicationDbContext automatically applies
/// the tenant WHERE clause, so no TenantId filtering is needed here.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        => await _context.Products.AddAsync(product, cancellationToken);

    public void Update(Product product)
        => _context.Products.Update(product);

    public void Remove(Product product)
        => _context.Products.Remove(product);
}
