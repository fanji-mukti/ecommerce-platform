using ECommerce.Catalog.API.Features.Products;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.API.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit outbox/inbox tables — required for transactional outbox
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Price).HasColumnType("decimal(18,2)");
            b.Property(p => p.Category).HasMaxLength(100);
            b.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            b.Property(p => p.Description).HasMaxLength(2000);
        });
    }
}
