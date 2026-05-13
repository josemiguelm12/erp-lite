using ErpLite.Domain.Common;

namespace ErpLite.Domain.Entities;

public sealed class InvoiceItem : Entity
{
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
