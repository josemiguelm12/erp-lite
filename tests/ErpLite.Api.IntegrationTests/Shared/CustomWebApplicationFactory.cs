using ErpLite.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
using Testcontainers.PostgreSql;

namespace ErpLite.Api.IntegrationTests.Shared;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string LocalPostgresAdminConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=123456";
    private const string TestJwtIssuer = "ErpLite.Tests";
    private const string TestJwtAudience = "ErpLite.Api.Tests";
    private const string TestJwtSecret = "integration-tests-secret-key-with-at-least-32-chars";

    private readonly string fallbackDatabaseName = $"erp_lite_integration_{Guid.NewGuid():N}";
    private PostgreSqlContainer? database;
    private string databaseConnectionString = string.Empty;
    private bool usingLocalPostgresFallback;

    public TestSeedData SeedData { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = databaseConnectionString,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:AccessTokenMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = TestJwtIssuer,
                    ValidAudience = TestJwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });
        });
    }

    public async Task InitializeAsync()
    {
        await InitializeDatabaseAsync();
        await ResetDatabaseAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (database is not null)
        {
            await database.DisposeAsync();
        }

        if (usingLocalPostgresFallback)
        {
            await DropLocalDatabaseAsync();
        }

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

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ErpLiteDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
        SeedData = await TestDataSeeder.SeedAsync(scope.ServiceProvider);
    }

    public async Task<T> ExecuteScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            database = new PostgreSqlBuilder()
                .WithImage("postgres:17-alpine")
                .WithDatabase("erp_lite_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await database.StartAsync();
            databaseConnectionString = database.GetConnectionString();
        }
        catch (DockerUnavailableException)
        {
            usingLocalPostgresFallback = true;
            databaseConnectionString = $"Host=localhost;Port=5432;Database={fallbackDatabaseName};Username=postgres;Password=123456";
            await CreateLocalDatabaseAsync();
        }
    }

    private async Task CreateLocalDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(LocalPostgresAdminConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{fallbackDatabaseName}\"";
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropLocalDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(LocalPostgresAdminConnectionString);
        await connection.OpenAsync();

        await using (var terminateCommand = connection.CreateCommand())
        {
            terminateCommand.CommandText =
                """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @databaseName AND pid <> pg_backend_pid();
                """;
            terminateCommand.Parameters.AddWithValue("databaseName", fallbackDatabaseName);
            await terminateCommand.ExecuteNonQueryAsync();
        }

        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"DROP DATABASE IF EXISTS \"{fallbackDatabaseName}\"";
        await dropCommand.ExecuteNonQueryAsync();
    }
}
