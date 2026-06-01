namespace Application.Products.Queries.GetProducts;

/// <summary>
/// DTO (Data Transfer Object) returned by the GetProductsQuery.
/// DTOs live in the Application layer and are projection targets — they
/// decouple the API contract from the domain model.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    DateTime CreatedAt);
