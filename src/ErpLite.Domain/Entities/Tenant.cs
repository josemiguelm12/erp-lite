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
    public ICollection<Customer> Customers { get; set; } = new HashSet<Customer>();
    public ICollection<Product> Products { get; set; } = new HashSet<Product>();
    public ICollection<Invoice> Invoices { get; set; } = new HashSet<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
}
