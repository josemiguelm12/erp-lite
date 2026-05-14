using ErpLite.Domain.Entities;

namespace ErpLite.Application.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRolesForAuthenticationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRolesAndPermissionsAsync(Guid id, CancellationToken cancellationToken = default);
}
