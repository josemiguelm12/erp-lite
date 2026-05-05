namespace ErpLite.Application.DTOs;

public sealed record CreateTenantRequest(string Name, string? Slug);
public sealed record TenantResponse(Guid Id, string Name, string? Slug, bool IsActive, DateTime CreatedAt);
