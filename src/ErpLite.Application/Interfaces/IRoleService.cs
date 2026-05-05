using ErpLite.Application.Common;
using ErpLite.Application.DTOs;

namespace ErpLite.Application.Interfaces;

public interface IRoleService
{
    Task<Result<RoleResponse>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> AssignPermissionAsync(Guid roleId, AssignPermissionToRoleRequest request, CancellationToken cancellationToken = default);
}
