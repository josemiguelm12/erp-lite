using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpLite.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(ErpLiteDbContext dbContext, ITenantProvider tenantProvider)
    : Repository<Role>(dbContext, tenantProvider), IRoleRepository
{
    public Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(DbContext.Roles.Include(x => x.Permissions))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
