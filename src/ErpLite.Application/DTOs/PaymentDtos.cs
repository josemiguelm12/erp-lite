using ErpLite.Domain.Enums;

namespace ErpLite.Application.DTOs;

public sealed record PaymentResponse(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    DateTime PaymentDate,
    PaymentMethod Method,
    string? Reference,
    string? Notes,
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime? UpdatedAt,
    Guid? UpdatedBy);

public sealed record RegisterPaymentRequest(
    Guid InvoiceId,
    decimal Amount,
    DateTime PaymentDate,
    PaymentMethod Method,
    string? Reference,
    string? Notes);
