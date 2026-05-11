using ErpLite.Domain.Entities;

namespace ErpLite.Application.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<(IReadOnlyCollection<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);
}
