using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;

namespace ErpLite.Application.Services;

public sealed class AuthService(
    IRepository<Tenant> tenants,
    IUserRepository users,
    IRoleRepository roles,
    IRepository<Permission> permissions,
    IRepository<RefreshToken> refreshTokens,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork) : IAuthService
{
    private static readonly string[] OwnerPermissions =
    [
        "customers.read",
        "customers.create",
        "customers.update",
        "customers.delete",
        "products.read",
        "products.create",
        "products.update",
        "products.delete",
        "invoices.read",
        "invoices.create",
        "invoices.update",
        "invoices.delete",
        "payments.read",
        "payments.create",
        "payments.delete"
    ];

    public async Task<Result<AuthResponse>> RegisterTenantAsync(RegisterTenantRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.OwnerEmail.Trim().ToLowerInvariant();
        if (await users.GetByEmailWithRolesAsync(normalizedEmail, cancellationToken) is not null)
        {
            return Result<AuthResponse>.Failure("Email already registered.");
        }

        if (!string.IsNullOrWhiteSpace(request.Slug) &&
            await tenants.FirstOrDefaultAsync(x => x.Slug == request.Slug.Trim().ToLowerInvariant(), cancellationToken) is not null)
        {
            return Result<AuthResponse>.Failure("Tenant slug already exists.");
        }

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            Slug = string.IsNullOrWhiteSpace(request.Slug) ? null : request.Slug.Trim().ToLowerInvariant()
        };

        var ownerRole = new Role
        {
            Name = "Owner",
            TenantId = tenant.Id
        };

        foreach (var permissionName in OwnerPermissions)
        {
            var permission = await permissions.FirstOrDefaultAsync(x => x.Name == permissionName, cancellationToken);
            if (permission is null)
            {
                permission = new Permission { Name = permissionName };
                await permissions.AddAsync(permission, cancellationToken);
            }

            ownerRole.Permissions.Add(permission);
        }

        var user = new User
        {
            TenantId = tenant.Id,
            FullName = request.OwnerFullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password)
        };
        user.Roles.Add(ownerRole);

        await tenants.AddAsync(tenant, cancellationToken);
        await roles.AddAsync(ownerRole, cancellationToken);
        await users.AddAsync(user, cancellationToken);

        var authResponse = await CreateAuthResponseAsync(user, ["Owner"], cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(authResponse);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByEmailWithRolesAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || user.IsDeleted || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Failure("Invalid credentials.");
        }

        var roleNames = user.Roles.Select(x => x.Name).OrderBy(x => x).ToArray();
        var response = await CreateAuthResponseAsync(user, roleNames, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var storedRefreshToken = await refreshTokens.FirstOrDefaultAsync(
            x => x.Token == request.RefreshToken,
            cancellationToken);

        if (storedRefreshToken is null ||
            storedRefreshToken.IsRevoked ||
            storedRefreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure("Invalid refresh token.");
        }

        var user = await users.GetByIdWithRolesForAuthenticationAsync(storedRefreshToken.UserId, cancellationToken);
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            return Result<AuthResponse>.Failure("Invalid refresh token.");
        }

        storedRefreshToken.IsRevoked = true;

        var roleNames = user.Roles.Select(x => x.Name).OrderBy(x => x).ToArray();
        var response = await CreateAuthResponseAsync(user, roleNames, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(response);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken)
    {
        var accessToken = jwtTokenService.CreateAccessToken(user, roleNames);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = jwtTokenService.CreateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await refreshTokens.AddAsync(refreshToken, cancellationToken);

        return new AuthResponse(
            user.Id,
            user.TenantId,
            user.FullName,
            user.Email,
            roleNames,
            accessToken.AccessToken,
            refreshToken.Token,
            accessToken.ExpiresAt,
            refreshToken.ExpiresAt);
    }
}
