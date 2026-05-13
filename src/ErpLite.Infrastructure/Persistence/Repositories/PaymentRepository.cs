using ErpLite.Application.Interfaces;
using ErpLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpLite.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(ErpLiteDbContext dbContext, ITenantProvider tenantProvider)
    : Repository<Payment>(dbContext, tenantProvider), IPaymentRepository
{
    public async Task<IReadOnlyCollection<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await ApplyTenantFilter(DbContext.Payments)
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await ApplyTenantFilter(DbContext.Payments)
            .Where(x => x.InvoiceId == invoiceId)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
    }
}
