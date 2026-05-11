using ErpLite.Domain.Entities;

namespace ErpLite.Application.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<(IReadOnlyCollection<Customer> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);
}
