using ErpLite.Domain.Enums;

namespace ErpLite.Application.DTOs;

public sealed record InvoiceItemResponse(
    Guid Id,
    Guid ProductId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total);

public sealed record InvoiceListItemResponse(
    Guid Id,
    Guid CustomerId,
    string InvoiceNumber,
    InvoiceStatus Status,
    DateTime IssueDate,
    DateTime? DueDate,
    decimal Subtotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total,
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime? UpdatedAt,
    Guid? UpdatedBy);

public sealed record InvoiceResponse(
    Guid Id,
    Guid CustomerId,
    string InvoiceNumber,
    InvoiceStatus Status,
    DateTime IssueDate,
    DateTime? DueDate,
    decimal Subtotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total,
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime? UpdatedAt,
    Guid? UpdatedBy,
    IReadOnlyCollection<InvoiceItemResponse> Items);

public sealed record CreateInvoiceItemRequest(
    Guid ProductId,
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public sealed record CreateInvoiceRequest(
    Guid CustomerId,
    string InvoiceNumber,
    DateTime IssueDate,
    DateTime? DueDate,
    decimal TaxRate,
    IReadOnlyCollection<CreateInvoiceItemRequest> Items);

public sealed record UpdateInvoiceRequest(
    Guid CustomerId,
    string InvoiceNumber,
    DateTime IssueDate,
    DateTime? DueDate,
    decimal TaxRate,
    IReadOnlyCollection<CreateInvoiceItemRequest> Items);

public sealed record ChangeInvoiceStatusRequest(InvoiceStatus Status);
