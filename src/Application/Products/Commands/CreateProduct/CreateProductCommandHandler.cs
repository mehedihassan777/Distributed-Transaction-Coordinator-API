using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Repositories;
using MediatR;

namespace Application.Products.Commands.CreateProduct;

/// <summary>
/// Handles the CreateProductCommand. 
/// Resolves TenantId from ITenantService (set by JWT middleware) to guarantee
/// each product is scoped to the correct tenant without trusting the caller.
/// Publishes a domain event post-save to support the Outbox/RabbitMQ pipeline.
/// </summary>
public sealed class CreateProductCommandHandler
    : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly IEventPublisher _eventPublisher;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(
            _tenantService.TenantId,
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity);

        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new ProductCreatedEvent(product.Id, product.TenantId, product.Name), cancellationToken);

        return product.Id;
    }
}

/// <summary>
/// Integration event published to RabbitMQ after a product is created.
/// Downstream services (e.g., Inventory, Search) can subscribe to this event.
/// </summary>
public sealed record ProductCreatedEvent(Guid ProductId, Guid TenantId, string ProductName);
