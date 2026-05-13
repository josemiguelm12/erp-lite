using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;
using ErpLite.Domain.Enums;

namespace ErpLite.Application.Services;

public sealed class InvoiceService(
    IInvoiceRepository invoices,
    ICustomerRepository customers,
    IProductRepository products,
    ITenantProvider tenantProvider,
    IPermissionService permissionService,
    IUnitOfWork unitOfWork) : IInvoiceService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    public async Task<OperationResult<InvoiceResponse>> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("invoices.create", cancellationToken))
        {
            return OperationResult<InvoiceResponse>.Forbidden("Permission invoices.create is required.");
        }

        if (tenantProvider.TenantId is not Guid tenantId)
        {
            return OperationResult<InvoiceResponse>.Forbidden("Tenant context is required.");
        }

        var validationError = await ValidateRequestAsync(request.CustomerId, request.InvoiceNumber, request.Items, null, cancellationToken);
        if (validationError is not null)
        {
            return OperationResult<InvoiceResponse>.ValidationError(validationError);
        }

        var invoice = new Invoice
        {
            TenantId = tenantId,
            CustomerId = request.CustomerId,
            InvoiceNumber = request.InvoiceNumber.Trim(),
            Status = InvoiceStatus.Draft,
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            TaxRate = request.TaxRate
        };

        ApplyItemsAndTotals(invoice, request.Items);

        await invoices.AddAsync(invoice, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<InvoiceResponse>.Success(ToResponse(invoice));
    }

    public async Task<OperationResult> UpdateAsync(Guid id, UpdateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("invoices.update", cancellationToken))
        {
            return OperationResult.Forbidden("Permission invoices.update is required.");
        }

        var invoice = await invoices.GetByIdWithItemsAsync(id, cancellationToken);
        if (invoice is null)
        {
            return OperationResult.NotFound("Invoice not found.");
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            return OperationResult.ValidationError("Only draft invoices can be updated.");
        }

        var validationError = await ValidateRequestAsync(request.CustomerId, request.InvoiceNumber, request.Items, invoice.Id, cancellationToken);
        if (validationError is not null)
        {
            return OperationResult.ValidationError(validationError);
        }

        invoice.CustomerId = request.CustomerId;
        invoice.InvoiceNumber = request.InvoiceNumber.Trim();
        invoice.IssueDate = request.IssueDate;
        invoice.DueDate = request.DueDate;
        invoice.TaxRate = request.TaxRate;

        invoice.Items.Clear();
        ApplyItemsAndTotals(invoice, request.Items);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult<InvoiceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("invoices.read", cancellationToken))
        {
            return OperationResult<InvoiceResponse>.Forbidden("Permission invoices.read is required.");
        }

        var invoice = await invoices.GetByIdWithItemsAsync(id, cancellationToken);
        return invoice is null
            ? OperationResult<InvoiceResponse>.NotFound("Invoice not found.")
            : OperationResult<InvoiceResponse>.Success(ToResponse(invoice));
    }

    public async Task<OperationResult<PagedResponse<InvoiceListItemResponse>>> GetPagedAsync(
        int page,
        int pageSize,
        InvoiceStatus? status,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("invoices.read", cancellationToken))
        {
            return OperationResult<PagedResponse<InvoiceListItemResponse>>.Forbidden("Permission invoices.read is required.");
        }

        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var (items, totalCount) = await invoices.GetPagedAsync(normalizedPage, normalizedPageSize, status, cancellationToken);
        var response = new PagedResponse<InvoiceListItemResponse>(
            items.Select(ToListItemResponse).ToArray(),
            totalCount,
            normalizedPage,
            normalizedPageSize,
            CalculateTotalPages(totalCount, normalizedPageSize));

        return OperationResult<PagedResponse<InvoiceListItemResponse>>.Success(response);
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("invoices.delete", cancellationToken))
        {
            return OperationResult.Forbidden("Permission invoices.delete is required.");
        }

        var invoice = await invoices.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return OperationResult.NotFound("Invoice not found.");
        }

        if (invoice.Status is not (InvoiceStatus.Draft or InvoiceStatus.Cancelled))
        {
            return OperationResult.ValidationError("Only draft or cancelled invoices can be deleted.");
        }

        invoice.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> ChangeStatusAsync(Guid id, ChangeInvoiceStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("invoices.update", cancellationToken))
        {
            return OperationResult.Forbidden("Permission invoices.update is required.");
        }

        var invoice = await invoices.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return OperationResult.NotFound("Invoice not found.");
        }

        if (!IsValidStatusTransition(invoice.Status, request.Status))
        {
            return OperationResult.ValidationError("Invalid invoice status transition.");
        }

        invoice.Status = request.Status;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private async Task<string?> ValidateRequestAsync(
        Guid customerId,
        string invoiceNumber,
        IReadOnlyCollection<CreateInvoiceItemRequest> items,
        Guid? excludedInvoiceId,
        CancellationToken cancellationToken)
    {
        if (await customers.GetByIdAsync(customerId, cancellationToken) is null)
        {
            return "Customer not found.";
        }

        if (await invoices.InvoiceNumberExistsAsync(invoiceNumber.Trim(), excludedInvoiceId, cancellationToken))
        {
            return "Invoice number already exists.";
        }

        var productIds = items.Select(x => x.ProductId).Distinct().ToArray();
        var existingProducts = await products.ListAsync(x => productIds.Contains(x.Id), cancellationToken);
        var productMap = existingProducts.ToDictionary(x => x.Id);

        var missingProductId = productIds.FirstOrDefault(productId => !productMap.ContainsKey(productId));
        if (missingProductId != Guid.Empty)
        {
            return "One or more invoice items reference products that were not found.";
        }

        return null;
    }

    private static void ApplyItemsAndTotals(Invoice invoice, IReadOnlyCollection<CreateInvoiceItemRequest> items)
    {
        foreach (var itemRequest in items)
        {
            var total = RoundMoney(itemRequest.Quantity * itemRequest.UnitPrice);
            invoice.Items.Add(new InvoiceItem
            {
                ProductId = itemRequest.ProductId,
                Description = itemRequest.Description.Trim(),
                Quantity = itemRequest.Quantity,
                UnitPrice = itemRequest.UnitPrice,
                Total = total
            });
        }

        invoice.Subtotal = RoundMoney(invoice.Items.Sum(x => x.Total));
        invoice.TaxAmount = RoundMoney(invoice.Subtotal * (invoice.TaxRate / 100m));
        invoice.Total = RoundMoney(invoice.Subtotal + invoice.TaxAmount);
    }

    private static bool IsValidStatusTransition(InvoiceStatus currentStatus, InvoiceStatus newStatus)
    {
        if (newStatus == InvoiceStatus.Cancelled)
        {
            return currentStatus != InvoiceStatus.Cancelled;
        }

        return currentStatus switch
        {
            InvoiceStatus.Draft => newStatus == InvoiceStatus.Sent,
            InvoiceStatus.Sent => newStatus == InvoiceStatus.Paid,
            _ => false
        };
    }

    private static InvoiceResponse ToResponse(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.CustomerId,
            invoice.InvoiceNumber,
            invoice.Status,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Subtotal,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoice.Total,
            invoice.CreatedAt,
            invoice.CreatedBy,
            invoice.UpdatedAt,
            invoice.UpdatedBy,
            invoice.Items
                .OrderBy(x => x.Id)
                .Select(x => new InvoiceItemResponse(x.Id, x.ProductId, x.Description, x.Quantity, x.UnitPrice, x.Total))
                .ToArray());

    private static InvoiceListItemResponse ToListItemResponse(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.CustomerId,
            invoice.InvoiceNumber,
            invoice.Status,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Subtotal,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoice.Total,
            invoice.CreatedAt,
            invoice.CreatedBy,
            invoice.UpdatedAt,
            invoice.UpdatedBy);

    private static int NormalizePage(int page) => page < DefaultPage ? DefaultPage : page;

    private static int NormalizePageSize(int pageSize) => pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

    private static int CalculateTotalPages(int totalCount, int pageSize) => totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

    private static decimal RoundMoney(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
