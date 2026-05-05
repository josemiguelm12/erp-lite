using ErpLite.Domain.Common;

namespace ErpLite.Domain.Entities;

public sealed class Tenant : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new HashSet<User>();
    public ICollection<Role> Roles { get; set; } = new HashSet<Role>();
}
