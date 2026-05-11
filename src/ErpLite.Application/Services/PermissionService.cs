using ErpLite.Application.Interfaces;

namespace ErpLite.Application.Services;

public sealed class PermissionService(
    ICurrentUserService currentUserService,
    IUserRepository users) : IPermissionService
{
    public async Task<bool> HasPermissionAsync(string permissionName, CancellationToken cancellationToken = default)
    {
        if (currentUserService.UserId is not Guid userId)
        {
            return false;
        }

        var user = await users.GetByIdWithRolesAndPermissionsAsync(userId, cancellationToken);
        return user is not null &&
            user.Roles.SelectMany(x => x.Permissions).Any(x => x.Name == permissionName);
    }
}
