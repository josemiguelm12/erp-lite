namespace ErpLite.Application.DTOs;

public sealed record RegisterUserRequest(string FullName, string Email, string Password);
public sealed record AssignRoleToUserRequest(Guid RoleId);
public sealed record UserResponse(Guid Id, Guid TenantId, string FullName, string Email, bool IsActive);
