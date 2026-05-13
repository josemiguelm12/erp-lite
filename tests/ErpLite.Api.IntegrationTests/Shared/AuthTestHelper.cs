using System.Net.Http.Headers;
using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace ErpLite.Api.IntegrationTests.Shared;

public static class AuthTestHelper
{
    public static async Task<string> GetAccessTokenAsync(
        CustomWebApplicationFactory factory,
        SeededUser user,
        CancellationToken cancellationToken = default)
    {
        return await factory.ExecuteScopeAsync(async serviceProvider =>
        {
            var users = serviceProvider.GetRequiredService<IUserRepository>();
            var tokenService = serviceProvider.GetRequiredService<IJwtTokenService>();

            var dbUser = await users.GetByEmailWithRolesAsync(user.Email, cancellationToken)
                ?? throw new InvalidOperationException($"Seeded user '{user.Email}' was not found.");

            var roleNames = dbUser.Roles
                .Select(role => role.Name)
                .OrderBy(roleName => roleName)
                .ToArray();

            return tokenService.CreateAccessToken(dbUser, roleNames).AccessToken;
        });
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        CustomWebApplicationFactory factory,
        SeededUser user,
        CancellationToken cancellationToken = default)
    {
        var client = factory.CreateApiClient();
        var accessToken = await GetAccessTokenAsync(factory, user, cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
