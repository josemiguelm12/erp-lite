using ErpLite.Domain.Common;
using ErpLite.Domain.Enums;

namespace ErpLite.Domain.Entities;

public sealed class Payment : AuditableEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
}
