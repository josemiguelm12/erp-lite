using ErpLite.Application.Common;
using ErpLite.Application.DTOs;

namespace ErpLite.Application.Interfaces;

public interface IPaymentService
{
    Task<OperationResult<PaymentResponse>> RegisterAsync(RegisterPaymentRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<IReadOnlyCollection<PaymentResponse>>> GetByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<OperationResult<PaymentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
