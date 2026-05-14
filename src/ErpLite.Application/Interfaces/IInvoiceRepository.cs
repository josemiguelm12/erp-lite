using ErpLite.Domain.Entities;
using ErpLite.Domain.Enums;

namespace ErpLite.Application.Interfaces;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<Invoice?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<Invoice> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        InvoiceStatus? status,
        CancellationToken cancellationToken = default);
    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, Guid? excludedInvoiceId = null, CancellationToken cancellationToken = default);
    Task DeleteItemsByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task AddItemsAsync(IEnumerable<InvoiceItem> items, CancellationToken cancellationToken = default);
    Task<bool> UpdateDetailsAsync(
        Guid id,
        Guid customerId,
        string invoiceNumber,
        DateTime issueDate,
        DateTime? dueDate,
        decimal subtotal,
        decimal taxRate,
        decimal taxAmount,
        decimal total,
        CancellationToken cancellationToken = default);
}
