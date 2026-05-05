using ErpLite.Application.Common;
using ErpLite.Application.DTOs;

namespace ErpLite.Application.Interfaces;

public interface IUserService
{
    Task<Result<UserResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> AssignRoleAsync(Guid userId, AssignRoleToUserRequest request, CancellationToken cancellationToken = default);
}
