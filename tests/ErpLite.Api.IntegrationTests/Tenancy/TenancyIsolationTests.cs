using ErpLite.Api.IntegrationTests.Shared;
using System.Net;
using System.Net.Http.Json;
using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using FluentAssertions;

namespace ErpLite.Api.IntegrationTests.Tenancy;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class TenancyIsolationTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TenantA_CreatesCustomer_TenantBDoesNotSeeItInList()
    {
        var tenantAClient = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var tenantBClient = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.SecondaryOwner);

        var createResponse = await tenantAClient.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("Tenant A Customer", "tenant.a.customer@test.local", null, null));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var listResponse = await tenantBClient.GetAsync("/api/customers?page=1&pageSize=20");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listPayload = await listResponse.Content.ReadFromJsonAsync<PagedResponse<CustomerResponse>>();
        listPayload!.Items.Should().NotContain(x => x.Id == created!.Id);
    }

    [Fact]
    public async Task TenantB_CannotGetCustomerByIdFromTenantA()
    {
        var tenantAClient = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var tenantBClient = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.SecondaryOwner);

        var createResponse = await tenantAClient.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("Tenant A Customer", "tenant.a.customer@test.local", null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var getResponse = await tenantBClient.GetAsync($"/api/customers/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantB_CannotDeleteCustomerFromTenantA()
    {
        var tenantAClient = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var tenantBClient = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.SecondaryOwner);

        var createResponse = await tenantAClient.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("Tenant A Customer", "tenant.a.customer@test.local", null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var deleteResponse = await tenantBClient.DeleteAsync($"/api/customers/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownerGetResponse = await tenantAClient.GetAsync($"/api/customers/{created.Id}");
        ownerGetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
