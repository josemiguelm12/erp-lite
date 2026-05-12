using System.Net.Http.Headers;
using System.Net.Http.Json;
using ErpLite.Application.DTOs;

namespace ErpLite.Api.IntegrationTests.Shared;

public static class AuthTestHelper
{
    public static async Task<string> GetAccessTokenAsync(
        HttpClient client,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Login response payload was empty.");

        return payload.AccessToken;
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        CustomWebApplicationFactory factory,
        SeededUser user,
        CancellationToken cancellationToken = default)
    {
        var client = factory.CreateApiClient();
        var accessToken = await GetAccessTokenAsync(client, user.Email, user.Password, cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
