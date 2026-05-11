using ErpLite.Application.Common;
using ErpLite.Application.DTOs;

namespace ErpLite.Application.Interfaces;

public interface IProductService
{
    Task<OperationResult<PagedResponse<ProductResponse>>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<OperationResult<ProductResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OperationResult<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
