using System.Linq.Expressions;
using ErpLite.Application.Interfaces;
using ErpLite.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ErpLite.Infrastructure.Persistence.Repositories;

public class Repository<T>(ErpLiteDbContext dbContext, ITenantProvider tenantProvider) : IRepository<T>
    where T : Entity
{
    protected ErpLiteDbContext DbContext => dbContext;

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(dbContext.Set<T>()).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(dbContext.Set<T>()).FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = ApplyTenantFilter(dbContext.Set<T>());
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query.ToListAsync(cancellationToken);
    }

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        dbContext.Set<T>().Add(entity);
        return Task.CompletedTask;
    }

    protected IQueryable<T> ApplyTenantFilter(IQueryable<T> query)
    {
        if (typeof(ITenantScoped).IsAssignableFrom(typeof(T)))
        {
            if (tenantProvider.TenantId is not Guid tenantId)
            {
                return query.Where(_ => false);
            }

            query = query.Where(entity => EF.Property<Guid>(entity, nameof(ITenantScoped.TenantId)) == tenantId);
        }

        if (typeof(AuditableEntity).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(entity => !EF.Property<bool>(entity, nameof(AuditableEntity.IsDeleted)));
        }

        return query;
    }
}
