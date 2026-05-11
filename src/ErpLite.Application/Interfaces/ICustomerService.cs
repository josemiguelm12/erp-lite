using ErpLite.Application.Common;
using ErpLite.Application.DTOs;

namespace ErpLite.Application.Interfaces;

public interface ICustomerService
{
    Task<OperationResult<PagedResponse<CustomerResponse>>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<OperationResult<CustomerResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OperationResult<CustomerResponse>> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
