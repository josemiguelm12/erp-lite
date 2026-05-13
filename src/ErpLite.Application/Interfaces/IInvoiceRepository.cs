using ErpLite.Domain.Entities;
using ErpLite.Domain.Enums;

namespace ErpLite.Application.Interfaces;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<Invoice?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<Invoice> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        InvoiceStatus? status,
        CancellationToken cancellationToken = default);
    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, Guid? excludedInvoiceId = null, CancellationToken cancellationToken = default);
}
