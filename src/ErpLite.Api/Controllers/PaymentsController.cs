using ErpLite.Api.Extensions;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpLite.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> Register(
        [FromBody] RegisterPaymentRequest request,
        [FromServices] IValidator<RegisterPaymentRequest> validator,
        CancellationToken cancellationToken)
    {
        if (await validator.ToBadRequestAsync(request, cancellationToken) is { } badRequest)
        {
            return badRequest;
        }

        var result = await paymentService.RegisterAsync(request, cancellationToken);
        return result.Status == Application.Common.OperationStatus.Success
            ? Created($"/api/payments/{result.Value!.Id}", result.Value)
            : result.ToActionResult();
    }

    [HttpGet("invoice/{invoiceId:guid}")]
    public async Task<ActionResult<IReadOnlyCollection<PaymentResponse>>> GetByInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await paymentService.GetByInvoiceAsync(invoiceId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await paymentService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await paymentService.DeleteAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
