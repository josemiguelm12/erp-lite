namespace ErpLite.Api.IntegrationTests.Shared;

[CollectionDefinition(Name)]
public sealed class ApiIntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "api-integration";
}
