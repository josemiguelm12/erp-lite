using ErpLite.Domain.Common;

namespace ErpLite.Domain.Entities;

public sealed class Permission : Entity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<Role> Roles { get; set; } = new HashSet<Role>();
}
