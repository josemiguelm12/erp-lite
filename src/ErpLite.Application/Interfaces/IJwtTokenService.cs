using ErpLite.Domain.Entities;

namespace ErpLite.Application.Interfaces;

public sealed record JwtTokenResult(string AccessToken, DateTime ExpiresAt);

public interface IJwtTokenService
{
    JwtTokenResult CreateAccessToken(User user, IReadOnlyCollection<string> roles);
    string CreateRefreshToken();
}
