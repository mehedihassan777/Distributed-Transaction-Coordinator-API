namespace DistributedTransactionCoordinator.Application.Common.DTOs;

/// <summary>
/// Data Transfer Object for Product read operations.
/// DTOs prevent leaking domain entities to the presentation layer.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
