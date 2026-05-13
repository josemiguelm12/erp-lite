using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;
using ErpLite.Domain.Enums;

namespace ErpLite.Application.Services;

public sealed class PaymentService(
    IPaymentRepository payments,
    IInvoiceRepository invoices,
    ITenantProvider tenantProvider,
    IPermissionService permissionService,
    IUnitOfWork unitOfWork) : IPaymentService
{
    public async Task<OperationResult<PaymentResponse>> RegisterAsync(RegisterPaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("payments.create", cancellationToken))
        {
            return OperationResult<PaymentResponse>.Forbidden("Permission payments.create is required.");
        }

        if (tenantProvider.TenantId is not Guid tenantId)
        {
            return OperationResult<PaymentResponse>.Forbidden("Tenant context is required.");
        }

        var invoice = await invoices.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return OperationResult<PaymentResponse>.NotFound("Invoice not found.");
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return OperationResult<PaymentResponse>.ValidationError("Cancelled invoices cannot receive payments.");
        }

        var paidAmount = await payments.GetTotalByInvoiceIdAsync(request.InvoiceId, cancellationToken);
        var pendingAmount = decimal.Round(invoice.Total - paidAmount, 2, MidpointRounding.AwayFromZero);

        if (request.Amount > pendingAmount)
        {
            return OperationResult<PaymentResponse>.ValidationError("Payment amount exceeds the pending invoice balance.");
        }

        var payment = new Payment
        {
            TenantId = tenantId,
            InvoiceId = request.InvoiceId,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            Method = request.Method,
            Reference = NormalizeOptional(request.Reference),
            Notes = NormalizeOptional(request.Notes)
        };

        await payments.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<PaymentResponse>.Success(ToResponse(payment));
    }

    public async Task<OperationResult<IReadOnlyCollection<PaymentResponse>>> GetByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("payments.read", cancellationToken))
        {
            return OperationResult<IReadOnlyCollection<PaymentResponse>>.Forbidden("Permission payments.read is required.");
        }

        if (await invoices.GetByIdAsync(invoiceId, cancellationToken) is null)
        {
            return OperationResult<IReadOnlyCollection<PaymentResponse>>.NotFound("Invoice not found.");
        }

        var paymentItems = await payments.GetByInvoiceIdAsync(invoiceId, cancellationToken);
        return OperationResult<IReadOnlyCollection<PaymentResponse>>.Success(paymentItems.Select(ToResponse).ToArray());
    }

    public async Task<OperationResult<PaymentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("payments.read", cancellationToken))
        {
            return OperationResult<PaymentResponse>.Forbidden("Permission payments.read is required.");
        }

        var payment = await payments.GetByIdAsync(id, cancellationToken);
        return payment is null
            ? OperationResult<PaymentResponse>.NotFound("Payment not found.")
            : OperationResult<PaymentResponse>.Success(ToResponse(payment));
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("payments.delete", cancellationToken))
        {
            return OperationResult.Forbidden("Permission payments.delete is required.");
        }

        var payment = await payments.GetByIdAsync(id, cancellationToken);
        if (payment is null)
        {
            return OperationResult.NotFound("Payment not found.");
        }

        payment.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private static PaymentResponse ToResponse(Payment payment) =>
        new(
            payment.Id,
            payment.InvoiceId,
            payment.Amount,
            payment.PaymentDate,
            payment.Method,
            payment.Reference,
            payment.Notes,
            payment.CreatedAt,
            payment.CreatedBy,
            payment.UpdatedAt,
            payment.UpdatedBy);

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
