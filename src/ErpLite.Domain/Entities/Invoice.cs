using ErpLite.Domain.Common;
using ErpLite.Domain.Enums;

namespace ErpLite.Domain.Entities;

public sealed class Invoice : AuditableEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<InvoiceItem> Items { get; set; } = new HashSet<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
}
