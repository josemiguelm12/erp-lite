using ErpLite.Api.IntegrationTests.Shared;
using ErpLite.Application.DTOs;
using ErpLite.Domain.Entities;
using ErpLite.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace ErpLite.Api.IntegrationTests.Auth;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class AuthEndpointsTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private static readonly string[] FullPermissions =
    [
        "customers.create",
        "customers.delete",
        "customers.read",
        "customers.update",
        "invoices.create",
        "invoices.delete",
        "invoices.read",
        "invoices.update",
        "payments.create",
        "payments.delete",
        "payments.read",
        "products.create",
        "products.delete",
        "products.read",
        "products.update"
    ];

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTenantNameAndPermissions()
    {
        var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(factory.SeedData.PrimaryOwner.Email, factory.SeedData.PrimaryOwner.Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
        payload.Should().NotBeNull();
        payload!.TenantName.Should().Be("Primary Tenant");
        payload.Roles.Should().BeEquivalentTo(["Owner"]);
        payload.Permissions.Should().BeEquivalentTo(FullPermissions);
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsTenantNameAndPermissions()
    {
        var client = factory.CreateApiClient();
        var request = new RegisterTenantRequest(
            "Acme HQ",
            "acme-hq",
            "Acme Owner",
            "owner@acme.test",
            TestUsers.DefaultPassword);

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
        payload.Should().NotBeNull();
        payload!.TenantName.Should().Be(request.Name);
        payload.Roles.Should().BeEquivalentTo(["Owner"]);
        payload.Permissions.Should().BeEquivalentTo(FullPermissions);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsOkWithRotatedTokens()
    {
        var client = factory.CreateApiClient();
        var login = await LoginAsync(client, factory.SeedData.PrimaryOwner);

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
        payload.Should().NotBeNull();
        payload!.UserId.Should().Be(login.UserId);
        payload.TenantId.Should().Be(login.TenantId);
        payload.TenantName.Should().Be("Primary Tenant");
        payload.Permissions.Should().BeEquivalentTo(FullPermissions);
        payload.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.RefreshToken.Should().NotBeNullOrWhiteSpace();
        payload.RefreshToken.Should().NotBe(login.RefreshToken);

        await factory.ExecuteScopeAsync(async serviceProvider =>
        {
            var dbContext = serviceProvider.GetRequiredService<ErpLiteDbContext>();
            var oldToken = await dbContext.RefreshTokens.SingleAsync(x => x.Token == login.RefreshToken);
            var newToken = await dbContext.RefreshTokens.SingleAsync(x => x.Token == payload.RefreshToken);

            oldToken.IsRevoked.Should().BeTrue();
            newToken.IsRevoked.Should().BeFalse();
            newToken.UserId.Should().Be(login.UserId);

            return true;
        });
    }

    [Fact]
    public async Task Refresh_ReusingPreviousRefreshToken_ReturnsUnauthorized()
    {
        var client = factory.CreateApiClient();
        var login = await LoginAsync(client, factory.SeedData.PrimaryOwner);

        var firstRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken));
        var secondRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken));

        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
        secondRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest("unknown-refresh-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithEmptyToken_ReturnsBadRequest()
    {
        var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(string.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ReturnsUnauthorized()
    {
        var client = factory.CreateApiClient();
        var login = await LoginAsync(client, factory.SeedData.PrimaryOwner);
        await UpdateRefreshTokenAsync(login.RefreshToken, token => token.IsRevoked = true);

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized()
    {
        var client = factory.CreateApiClient();
        var login = await LoginAsync(client, factory.SeedData.PrimaryOwner);
        await UpdateRefreshTokenAsync(login.RefreshToken, token => token.ExpiresAt = DateTime.UtcNow.AddMinutes(-1));

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithInactiveUser_ReturnsUnauthorized()
    {
        var client = factory.CreateApiClient();
        var login = await LoginAsync(client, factory.SeedData.PrimaryOwner);
        await UpdateUserAsync(login.UserId, user => user.IsActive = false);

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithDeletedUser_ReturnsUnauthorized()
    {
        var client = factory.CreateApiClient();
        var login = await LoginAsync(client, factory.SeedData.PrimaryOwner);
        await UpdateUserAsync(login.UserId, user => user.IsDeleted = true);

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<AuthResponse> LoginAsync(HttpClient client, SeededUser user)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, user.Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
        payload.Should().NotBeNull();

        return payload!;
    }

    private async Task UpdateRefreshTokenAsync(string refreshToken, Action<RefreshToken> update)
    {
        await factory.ExecuteScopeAsync(async serviceProvider =>
        {
            var dbContext = serviceProvider.GetRequiredService<ErpLiteDbContext>();
            var token = await dbContext.RefreshTokens.SingleAsync(x => x.Token == refreshToken);
            update(token);
            await dbContext.SaveChangesAsync();

            return true;
        });
    }

    private async Task UpdateUserAsync(Guid userId, Action<User> update)
    {
        await factory.ExecuteScopeAsync(async serviceProvider =>
        {
            var dbContext = serviceProvider.GetRequiredService<ErpLiteDbContext>();
            var user = await dbContext.Users.SingleAsync(x => x.Id == userId);
            update(user);
            await dbContext.SaveChangesAsync();

            return true;
        });
    }
}
