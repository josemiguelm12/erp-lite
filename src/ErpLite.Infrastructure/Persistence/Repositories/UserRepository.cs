using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpLite.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(ErpLiteDbContext dbContext, ITenantProvider tenantProvider)
    : Repository<User>(dbContext, tenantProvider), IUserRepository
{
    public Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default)
    {
        return DbContext.Users
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(DbContext.Users.Include(x => x.Roles))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<User?> GetByIdWithRolesForAuthenticationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.Users
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<User?> GetByIdWithRolesAndPermissionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(DbContext.Users
                .Include(x => x.Roles)
                .ThenInclude(x => x.Permissions))
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
