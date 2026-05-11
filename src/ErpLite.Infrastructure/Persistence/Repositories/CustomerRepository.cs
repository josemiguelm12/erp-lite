using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpLite.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(ErpLiteDbContext dbContext, ITenantProvider tenantProvider)
    : Repository<Customer>(dbContext, tenantProvider), ICustomerRepository
{
    public async Task<(IReadOnlyCollection<Customer> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyTenantFilter(DbContext.Customers).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, pattern) ||
                (x.Email != null && EF.Functions.ILike(x.Email, pattern)) ||
                (x.Phone != null && EF.Functions.ILike(x.Phone, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
