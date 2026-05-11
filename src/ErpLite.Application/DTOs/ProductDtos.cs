namespace ErpLite.Application.DTOs;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock);

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock);
