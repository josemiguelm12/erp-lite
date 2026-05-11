namespace ErpLite.Application.DTOs;

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime? UpdatedAt,
    Guid? UpdatedBy);

public sealed record CreateCustomerRequest(
    string Name,
    string? Email,
    string? Phone,
    string? Address);

public sealed record UpdateCustomerRequest(
    string Name,
    string? Email,
    string? Phone,
    string? Address);
