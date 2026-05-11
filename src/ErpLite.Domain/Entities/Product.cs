using ErpLite.Domain.Common;

namespace ErpLite.Domain.Entities;

public sealed class Product : AuditableEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
