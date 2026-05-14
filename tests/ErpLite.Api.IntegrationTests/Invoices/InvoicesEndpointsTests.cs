using ErpLite.Api.IntegrationTests.Shared;
using System.Net;
using System.Net.Http.Json;
using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using ErpLite.Domain.Enums;
using FluentAssertions;

namespace ErpLite.Api.IntegrationTests.Invoices;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class InvoicesEndpointsTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_WithValidToken_ReturnsCreatedWithExpectedData()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = CreateInvoiceRequest("INV-CREATED-001");

        var response = await client.PostAsJsonAsync("/api/invoices", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().NotBeEmpty();
        payload.CustomerId.Should().Be(request.CustomerId);
        payload.InvoiceNumber.Should().Be(request.InvoiceNumber);
        payload.Status.Should().Be(InvoiceStatus.Draft);
        payload.Items.Should().HaveCount(2);
        payload.Subtotal.Should().Be(40m);
        payload.TaxRate.Should().Be(18m);
        payload.TaxAmount.Should().Be(7.20m);
        payload.Total.Should().Be(47.20m);
    }

    [Fact]
    public async Task Post_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/invoices", CreateInvoiceRequest("INV-NO-TOKEN"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WithInvalidPayload_ReturnsBadRequest()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new CreateInvoiceRequest(
            Guid.Empty,
            "INV-INVALID",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30),
            18m,
            [
                new CreateInvoiceItemRequest(Guid.Empty, string.Empty, -1m, 10m)
            ]);

        var response = await client.PostAsJsonAsync("/api/invoices", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("CustomerId");
        content.Should().Contain("ProductId");
        content.Should().Contain("Description");
        content.Should().Contain("Quantity");
    }

    [Fact]
    public async Task GetPaged_WithValidToken_ReturnsPagedList()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync("/api/invoices?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<InvoiceListItemResponse>>();
        payload.Should().NotBeNull();
        payload!.Page.Should().Be(1);
        payload.PageSize.Should().Be(10);
        payload.Items.Should().Contain(x => x.Id == factory.SeedData.DraftInvoiceId);
        payload.Items.Should().NotContain(x => x.Id == factory.SeedData.CrossTenantInvoiceId);
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsInvoiceWithItems()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync($"/api/invoices/{factory.SeedData.DraftInvoiceId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(factory.SeedData.DraftInvoiceId);
        payload.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_WithOtherTenantId_ReturnsNotFound()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync($"/api/invoices/{factory.SeedData.CrossTenantInvoiceId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_WhenDraft_UpdatesInvoice()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var createResponse = await client.PostAsJsonAsync("/api/invoices", CreateInvoiceRequest("INV-DRAFT-TO-UPDATE"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        createdInvoice.Should().NotBeNull();

        var request = new UpdateInvoiceRequest(
            factory.SeedData.ActiveCustomerId,
            "INV-UPDATED-DRAFT",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(45),
            10m,
            [
                new CreateInvoiceItemRequest(factory.SeedData.ActiveProductId, "Updated item", 3m, 12m)
            ]);

        var updateResponse = await client.PutAsJsonAsync($"/api/invoices/{createdInvoice!.Id}", request);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/invoices/{createdInvoice.Id}");
        var payload = await getResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        payload.Should().NotBeNull();
        payload!.InvoiceNumber.Should().Be(request.InvoiceNumber);
        payload.Subtotal.Should().Be(36m);
        payload.TaxAmount.Should().Be(3.60m);
        payload.Total.Should().Be(39.60m);
        payload.Items.Should().ContainSingle(x => x.Description == "Updated item");
    }

    [Fact]
    public async Task Put_WhenSent_ReturnsBadRequest()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.PutAsJsonAsync(
            $"/api/invoices/{factory.SeedData.SentInvoiceId}",
            CreateUpdateInvoiceRequest("INV-SENT-UPDATE"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchStatus_WithValidTransition_ReturnsNoContent()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new ChangeInvoiceStatusRequest(InvoiceStatus.Sent);

        var response = await client.PatchAsJsonAsync($"/api/invoices/{factory.SeedData.DraftInvoiceId}/status", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/invoices/{factory.SeedData.DraftInvoiceId}");
        var payload = await getResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        payload!.Status.Should().Be(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task PatchStatus_WithInvalidTransition_ReturnsBadRequest()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new ChangeInvoiceStatusRequest(InvoiceStatus.Draft);

        var response = await client.PatchAsJsonAsync($"/api/invoices/{factory.SeedData.PaidInvoiceId}/status", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_WhenDraft_SoftDeletesInvoice_AndSubsequentGetReturnsNotFound()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var deleteResponse = await client.DeleteAsync($"/api/invoices/{factory.SeedData.DraftInvoiceId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/invoices/{factory.SeedData.DraftInvoiceId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var listResponse = await client.GetAsync("/api/invoices?page=1&pageSize=20");
        var listPayload = await listResponse.Content.ReadFromJsonAsync<PagedResponse<InvoiceListItemResponse>>();
        listPayload!.Items.Should().NotContain(x => x.Id == factory.SeedData.DraftInvoiceId);
    }

    [Fact]
    public async Task Delete_WhenPaid_ReturnsBadRequest()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.DeleteAsync($"/api/invoices/{factory.SeedData.PaidInvoiceId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPaged_WithoutReadPermission_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.RestrictedUser);

        var response = await client.GetAsync("/api/invoices?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private CreateInvoiceRequest CreateInvoiceRequest(string invoiceNumber) =>
        new(
            factory.SeedData.ActiveCustomerId,
            invoiceNumber,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30),
            18m,
            [
                new CreateInvoiceItemRequest(factory.SeedData.ActiveProductId, "First invoice item", 2m, 15m),
                new CreateInvoiceItemRequest(factory.SeedData.ActiveProductId, "Second invoice item", 1m, 10m)
            ]);

    private UpdateInvoiceRequest CreateUpdateInvoiceRequest(string invoiceNumber) =>
        new(
            factory.SeedData.ActiveCustomerId,
            invoiceNumber,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30),
            18m,
            [
                new CreateInvoiceItemRequest(factory.SeedData.ActiveProductId, "Updated invoice item", 1m, 10m)
            ]);
}
