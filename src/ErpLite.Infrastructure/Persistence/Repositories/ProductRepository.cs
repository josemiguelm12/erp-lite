using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpLite.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ErpLiteDbContext dbContext, ITenantProvider tenantProvider)
    : Repository<Product>(dbContext, tenantProvider), IProductRepository
{
    public async Task<(IReadOnlyCollection<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyTenantFilter(DbContext.Products).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, pattern) ||
                (x.Description != null && EF.Functions.ILike(x.Description, pattern)));
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
