using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;

namespace ErpLite.Application.Services;

public sealed class UserService(
    IUserRepository users,
    IRoleRepository roles,
    ITenantProvider tenantProvider,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IUserService
{
    public async Task<Result<UserResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        if (tenantProvider.TenantId is not Guid tenantId)
        {
            return Result<UserResponse>.Failure("Tenant context is required.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailWithRolesAsync(normalizedEmail, cancellationToken) is not null)
        {
            return Result<UserResponse>.Failure("Email already registered.");
        }

        var user = new User
        {
            TenantId = tenantId,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserResponse>.Success(new UserResponse(user.Id, user.TenantId, user.FullName, user.Email, user.IsActive));
    }

    public async Task<Result> AssignRoleAsync(Guid userId, AssignRoleToUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdWithRolesAsync(userId, cancellationToken);
        var role = await roles.GetByIdAsync(request.RoleId, cancellationToken);
        if (user is null || role is null)
        {
            return Result.Failure("User or role not found for current tenant.");
        }

        if (user.Roles.All(x => x.Id != role.Id))
        {
            user.Roles.Add(role);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
