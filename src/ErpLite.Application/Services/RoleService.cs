using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;

namespace ErpLite.Application.Services;

public sealed class RoleService(
    IRoleRepository roles,
    IRepository<Permission> permissions,
    ITenantProvider tenantProvider,
    IUnitOfWork unitOfWork) : IRoleService
{
    public async Task<Result<RoleResponse>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantProvider.TenantId is not Guid tenantId)
        {
            return Result<RoleResponse>.Failure("Tenant context is required.");
        }

        var normalizedName = request.Name.Trim();
        if (await roles.FirstOrDefaultAsync(x => x.Name == normalizedName, cancellationToken) is not null)
        {
            return Result<RoleResponse>.Failure("Role already exists for current tenant.");
        }

        var role = new Role { Name = normalizedName, TenantId = tenantId };
        await roles.AddAsync(role, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RoleResponse>.Success(new RoleResponse(role.Id, role.TenantId, role.Name, []));
    }

    public async Task<Result> AssignPermissionAsync(Guid roleId, AssignPermissionToRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await roles.GetByIdWithPermissionsAsync(roleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure("Role not found for current tenant.");
        }

        var permissionName = request.PermissionName.Trim().ToLowerInvariant();
        var permission = await permissions.FirstOrDefaultAsync(x => x.Name == permissionName, cancellationToken);
        if (permission is null)
        {
            permission = new Permission { Name = permissionName };
            await permissions.AddAsync(permission, cancellationToken);
        }

        if (role.Permissions.All(x => x.Id != permission.Id))
        {
            role.Permissions.Add(permission);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
