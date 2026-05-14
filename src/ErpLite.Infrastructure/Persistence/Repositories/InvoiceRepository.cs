using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;
using ErpLite.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpLite.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository(ErpLiteDbContext dbContext, ITenantProvider tenantProvider)
    : Repository<Invoice>(dbContext, tenantProvider), IInvoiceRepository
{
    public Task<Invoice?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(DbContext.Invoices)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Invoice?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(DbContext.Invoices
                .Include(x => x.Items.OrderBy(item => item.Id)))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyCollection<Invoice> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        InvoiceStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyTenantFilter(DbContext.Invoices).AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.IssueDate)
            .ThenBy(x => x.InvoiceNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, Guid? excludedInvoiceId = null, CancellationToken cancellationToken = default)
    {
        var normalizedInvoiceNumber = invoiceNumber.Trim();
        return ApplyTenantFilter(DbContext.Invoices)
            .AnyAsync(
                x => x.InvoiceNumber == normalizedInvoiceNumber &&
                    (!excludedInvoiceId.HasValue || x.Id != excludedInvoiceId.Value),
                cancellationToken);
    }

    public Task DeleteItemsByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return DbContext.InvoiceItems
            .Where(x => x.InvoiceId == invoiceId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task AddItemsAsync(IEnumerable<InvoiceItem> items, CancellationToken cancellationToken = default)
    {
        DbContext.InvoiceItems.AddRange(items);
        return Task.CompletedTask;
    }

    public async Task<bool> UpdateDetailsAsync(
        Guid id,
        Guid customerId,
        string invoiceNumber,
        DateTime issueDate,
        DateTime? dueDate,
        decimal subtotal,
        decimal taxRate,
        decimal taxAmount,
        decimal total,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await ApplyTenantFilter(DbContext.Invoices)
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(x => x.CustomerId, customerId)
                    .SetProperty(x => x.InvoiceNumber, invoiceNumber)
                    .SetProperty(x => x.IssueDate, issueDate)
                    .SetProperty(x => x.DueDate, dueDate)
                    .SetProperty(x => x.Subtotal, subtotal)
                    .SetProperty(x => x.TaxRate, taxRate)
                    .SetProperty(x => x.TaxAmount, taxAmount)
                    .SetProperty(x => x.Total, total),
                cancellationToken);

        return affectedRows > 0;
    }
}
