using ErpLite.Api.IntegrationTests.Shared;
using System.Net;
using System.Net.Http.Json;
using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using FluentAssertions;

namespace ErpLite.Api.IntegrationTests.Customers;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class CustomersEndpointsTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_WithValidToken_ReturnsCreatedWithExpectedData()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new CreateCustomerRequest("Created Customer", "created.customer@test.local", "809-555-0101", "Santo Domingo");

        var response = await client.PostAsJsonAsync("/api/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().NotBeEmpty();
        payload.Name.Should().Be(request.Name);
        payload.Email.Should().Be(request.Email);
        payload.Phone.Should().Be(request.Phone);
        payload.Address.Should().Be(request.Address);
    }

    [Fact]
    public async Task Post_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest("Created Customer", "created.customer@test.local", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WithInvalidPayload_ReturnsBadRequest()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new CreateCustomerRequest(string.Empty, "invalid-email", null, null);

        var response = await client.PostAsJsonAsync("/api/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Name");
        content.Should().Contain("Email");
    }

    [Fact]
    public async Task GetPaged_WithValidToken_ReturnsPagedList()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync("/api/customers?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<CustomerResponse>>();
        payload.Should().NotBeNull();
        payload!.Page.Should().Be(1);
        payload.PageSize.Should().Be(10);
        payload.Items.Should().Contain(x => x.Id == factory.SeedData.ActiveCustomerId);
        payload.Items.Should().NotContain(x => x.Id == factory.SeedData.DeletedCustomerId);
        payload.Items.Should().NotContain(x => x.Id == factory.SeedData.CrossTenantCustomerId);
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsCustomer()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync($"/api/customers/{factory.SeedData.ActiveCustomerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(factory.SeedData.ActiveCustomerId);
    }

    [Fact]
    public async Task GetById_WithOtherTenantId_ReturnsNotFound()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync($"/api/customers/{factory.SeedData.CrossTenantCustomerId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_WithValidPayload_UpdatesCustomer()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new UpdateCustomerRequest("Updated Customer", "updated.customer@test.local", "809-555-0202", "Santiago");

        var updateResponse = await client.PutAsJsonAsync($"/api/customers/{factory.SeedData.ActiveCustomerId}", request);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/customers/{factory.SeedData.ActiveCustomerId}");
        var payload = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        payload.Should().NotBeNull();
        payload!.Name.Should().Be(request.Name);
        payload.Email.Should().Be(request.Email);
        payload.Phone.Should().Be(request.Phone);
        payload.Address.Should().Be(request.Address);
    }

    [Fact]
    public async Task Delete_SoftDeletesCustomer_AndSubsequentGetReturnsNotFound()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var deleteResponse = await client.DeleteAsync($"/api/customers/{factory.SeedData.ActiveCustomerId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/customers/{factory.SeedData.ActiveCustomerId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var listResponse = await client.GetAsync("/api/customers?page=1&pageSize=20");
        var listPayload = await listResponse.Content.ReadFromJsonAsync<PagedResponse<CustomerResponse>>();
        listPayload!.Items.Should().NotContain(x => x.Id == factory.SeedData.ActiveCustomerId);
    }

    [Fact]
    public async Task GetPaged_WithoutReadPermission_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.RestrictedUser);

        var response = await client.GetAsync("/api/customers?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
