namespace ErpLite.Application.DTOs;

public sealed record RegisterTenantRequest(
    string Name,
    string? Slug,
    string OwnerFullName,
    string OwnerEmail,
    string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record AuthResponse(
    Guid UserId,
    Guid TenantId,
    string FullName,
    string Email,
    IReadOnlyCollection<string> Roles,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);
