namespace ErpLite.Application.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string permissionName, CancellationToken cancellationToken = default);
}
