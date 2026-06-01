using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for the Product entity.
/// Keeping configurations separate from the entity ensures Domain has
/// zero EF Core dependencies and configurations are easy to locate.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Price)
            .HasPrecision(18, 4);

        builder.Property(p => p.TenantId)
            .IsRequired();

        // Composite index for common tenant-scoped queries
        builder.HasIndex(p => new { p.TenantId, p.Name });
    }
}
