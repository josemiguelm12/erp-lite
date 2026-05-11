using ErpLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpLite.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Price).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Stock).HasDefaultValue(0);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
        builder.HasIndex(x => new { x.TenantId, x.Name });

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
