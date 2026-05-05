using ErpLite.Application.Common;
using ErpLite.Application.DTOs;

namespace ErpLite.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterTenantAsync(RegisterTenantRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
