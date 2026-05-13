using ErpLite.Domain.Entities;

namespace ErpLite.Application.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IReadOnlyCollection<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
