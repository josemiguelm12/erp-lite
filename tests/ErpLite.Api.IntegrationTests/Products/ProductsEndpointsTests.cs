using ErpLite.Api.IntegrationTests.Shared;
using System.Net;
using System.Net.Http.Json;
using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using FluentAssertions;

namespace ErpLite.Api.IntegrationTests.Products;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class ProductsEndpointsTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_WithValidToken_ReturnsCreatedWithExpectedData()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new CreateProductRequest("Created Product", "Created product description", 99.5m, 7);

        var response = await client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ProductResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().NotBeEmpty();
        payload.Name.Should().Be(request.Name);
        payload.Description.Should().Be(request.Description);
        payload.Price.Should().Be(request.Price);
        payload.Stock.Should().Be(request.Stock);
    }

    [Fact]
    public async Task Post_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest("Created Product", "desc", 10m, 1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WithInvalidPayload_ReturnsBadRequest()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new CreateProductRequest(string.Empty, "Invalid product", -1m, -2);

        var response = await client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Name");
        content.Should().Contain("Price");
        content.Should().Contain("Stock");
    }

    [Fact]
    public async Task GetPaged_WithValidToken_ReturnsPagedList()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync("/api/products?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        payload.Should().NotBeNull();
        payload!.Page.Should().Be(1);
        payload.PageSize.Should().Be(10);
        payload.Items.Should().Contain(x => x.Id == factory.SeedData.ActiveProductId);
        payload.Items.Should().NotContain(x => x.Id == factory.SeedData.DeletedProductId);
        payload.Items.Should().NotContain(x => x.Id == factory.SeedData.CrossTenantProductId);
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsProduct()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync($"/api/products/{factory.SeedData.ActiveProductId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ProductResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(factory.SeedData.ActiveProductId);
    }

    [Fact]
    public async Task GetById_WithOtherTenantId_ReturnsNotFound()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync($"/api/products/{factory.SeedData.CrossTenantProductId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_WithValidPayload_UpdatesProduct()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new UpdateProductRequest("Updated Product", "Updated description", 149.99m, 12);

        var updateResponse = await client.PutAsJsonAsync($"/api/products/{factory.SeedData.ActiveProductId}", request);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/products/{factory.SeedData.ActiveProductId}");
        var payload = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        payload.Should().NotBeNull();
        payload!.Name.Should().Be(request.Name);
        payload.Description.Should().Be(request.Description);
        payload.Price.Should().Be(request.Price);
        payload.Stock.Should().Be(request.Stock);
    }

    [Fact]
    public async Task Delete_SoftDeletesProduct_AndSubsequentGetReturnsNotFound()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var deleteResponse = await client.DeleteAsync($"/api/products/{factory.SeedData.ActiveProductId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/products/{factory.SeedData.ActiveProductId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var listResponse = await client.GetAsync("/api/products?page=1&pageSize=20");
        var listPayload = await listResponse.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        listPayload!.Items.Should().NotContain(x => x.Id == factory.SeedData.ActiveProductId);
    }

    [Fact]
    public async Task GetPaged_WithoutReadPermission_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.RestrictedUser);

        var response = await client.GetAsync("/api/products?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
