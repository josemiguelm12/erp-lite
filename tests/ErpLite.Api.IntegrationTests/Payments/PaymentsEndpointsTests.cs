using ErpLite.Api.IntegrationTests.Shared;
using System.Net;
using System.Net.Http.Json;
using ErpLite.Application.DTOs;
using ErpLite.Domain.Enums;
using FluentAssertions;

namespace ErpLite.Api.IntegrationTests.Payments;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PaymentsEndpointsTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_WithValidInvoice_ReturnsCreated()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new RegisterPaymentRequest(
            factory.SeedData.DraftInvoiceId,
            15m,
            DateTime.UtcNow.Date,
            PaymentMethod.BankTransfer,
            "PAY-CREATED-001",
            "Created payment");

        var response = await client.PostAsJsonAsync("/api/payments", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().NotBeEmpty();
        payload.InvoiceId.Should().Be(request.InvoiceId);
        payload.Amount.Should().Be(request.Amount);
        payload.Method.Should().Be(request.Method);
        payload.Reference.Should().Be(request.Reference);
        payload.Notes.Should().Be(request.Notes);
    }

    [Fact]
    public async Task Post_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync(
            "/api/payments",
            new RegisterPaymentRequest(factory.SeedData.DraftInvoiceId, 10m, DateTime.UtcNow.Date, PaymentMethod.Cash, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WithAmountExceedingPendingBalance_ReturnsBadRequest()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new RegisterPaymentRequest(
            factory.SeedData.DraftInvoiceId,
            999m,
            DateTime.UtcNow.Date,
            PaymentMethod.CreditCard,
            null,
            null);

        var response = await client.PostAsJsonAsync("/api/payments", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithCancelledInvoice_ReturnsBadRequest()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);
        var request = new RegisterPaymentRequest(
            factory.SeedData.CancelledInvoiceId,
            10m,
            DateTime.UtcNow.Date,
            PaymentMethod.Cash,
            null,
            null);

        var response = await client.PostAsJsonAsync("/api/payments", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetByInvoice_WithExistingInvoice_ReturnsPayments()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync($"/api/payments/invoice/{factory.SeedData.SentInvoiceId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<PaymentResponse>>();
        payload.Should().NotBeNull();
        payload!.Should().Contain(x => x.Id == factory.SeedData.ActivePaymentId);
    }

    [Fact]
    public async Task GetByInvoice_WithOtherTenantInvoice_ReturnsNotFound()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync($"/api/payments/invoice/{factory.SeedData.CrossTenantInvoiceId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsPayment()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var response = await client.GetAsync($"/api/payments/{factory.SeedData.ActivePaymentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(factory.SeedData.ActivePaymentId);
        payload.InvoiceId.Should().Be(factory.SeedData.SentInvoiceId);
    }

    [Fact]
    public async Task Delete_SoftDeletesPayment()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.PrimaryOwner);

        var deleteResponse = await client.DeleteAsync($"/api/payments/{factory.SeedData.ActivePaymentId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/payments/{factory.SeedData.ActivePaymentId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var listResponse = await client.GetAsync($"/api/payments/invoice/{factory.SeedData.SentInvoiceId}");
        var listPayload = await listResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<PaymentResponse>>();
        listPayload!.Should().NotContain(x => x.Id == factory.SeedData.ActivePaymentId);
    }

    [Fact]
    public async Task GetById_WithoutReadPermission_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateAuthenticatedClientAsync(factory, factory.SeedData.RestrictedUser);

        var response = await client.GetAsync($"/api/payments/{factory.SeedData.ActivePaymentId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
