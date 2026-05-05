using ErpLite.Application.Common;
using ErpLite.Application.DTOs;

namespace ErpLite.Application.Interfaces;

public interface ITenantService
{
    Task<Result<TenantResponse>> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
}
