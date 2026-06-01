using MediatR;
using DistributedTransactionCoordinator.Domain.Entities;
using DistributedTransactionCoordinator.Domain.Interfaces;

namespace DistributedTransactionCoordinator.Application.Products.Commands.CreateProduct;

/// <summary>
/// Command to create a new product.
/// Commands represent intent to mutate state and return the new entity Id.
/// </summary>
public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity
) : IRequest<Guid>;

/// <summary>
/// Handler for CreateProductCommand.
/// Orchestrates domain logic, persists via repository, and publishes the outbox event.
/// </summary>
public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    Domain.Interfaces.ITenantContext tenantContext)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(
            tenantContext.TenantId,
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity);

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
