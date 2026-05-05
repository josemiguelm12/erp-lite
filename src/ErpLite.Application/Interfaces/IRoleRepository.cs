using ErpLite.Domain.Entities;

namespace ErpLite.Application.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default);
}
