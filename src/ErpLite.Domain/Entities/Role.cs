using ErpLite.Domain.Common;

namespace ErpLite.Domain.Entities;

public sealed class Role : Entity, ITenantScoped
{
    public string Name { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
    public ICollection<User> Users { get; set; } = new HashSet<User>();
    public ICollection<Permission> Permissions { get; set; } = new HashSet<Permission>();
}
