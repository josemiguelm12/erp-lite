using ErpLite.Application.Common;
using ErpLite.Application.DTOs;
using ErpLite.Domain.Enums;

namespace ErpLite.Application.Interfaces;

public interface IInvoiceService
{
    Task<OperationResult<InvoiceResponse>> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(Guid id, UpdateInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<InvoiceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OperationResult<PagedResponse<InvoiceListItemResponse>>> GetPagedAsync(int page, int pageSize, InvoiceStatus? status, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OperationResult> ChangeStatusAsync(Guid id, ChangeInvoiceStatusRequest request, CancellationToken cancellationToken = default);
}
