namespace ErpLite.Application.DTOs;

public sealed record CreateRoleRequest(string Name);
public sealed record AssignPermissionToRoleRequest(string PermissionName);
public sealed record RoleResponse(Guid Id, Guid TenantId, string Name, IReadOnlyCollection<string> Permissions);
