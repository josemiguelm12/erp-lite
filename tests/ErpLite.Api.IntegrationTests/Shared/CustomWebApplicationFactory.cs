using ErpLite.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ErpLite.Api.IntegrationTests.Shared;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("erp_lite_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public TestSeedData SeedData { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = database.GetConnectionString(),
                ["Jwt:Issuer"] = "ErpLite.Tests",
                ["Jwt:Audience"] = "ErpLite.Api.Tests",
                ["Jwt:Secret"] = "integration-tests-secret-key-with-at-least-32-chars",
                ["Jwt:AccessTokenMinutes"] = "60"
            });
        });
    }

    public async Task InitializeAsync()
    {
        await database.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ErpLiteDbContext>();

        await dbContext.Database.MigrateAsync();
        SeedData = await TestDataSeeder.SeedAsync(scope.ServiceProvider);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await database.DisposeAsync();
        Dispose();
    }

    public HttpClient CreateApiClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }
}
