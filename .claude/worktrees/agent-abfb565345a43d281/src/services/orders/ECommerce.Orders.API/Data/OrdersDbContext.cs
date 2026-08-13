using ECommerce.Orders.API.Features.Orders;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Orders.API.Data;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderReadModel> OrderReadModels => Set<OrderReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit outbox/inbox tables — required for transactional outbox (D-07, ADR-0006)
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.CorrelationId);
            b.Property(o => o.CurrentState).IsRequired().HasMaxLength(50);
            b.Property(o => o.UserId).IsRequired().HasMaxLength(200);
            b.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

            // Owned collection for the write-side aggregate. This is the FIRST of two
            // independent OwnsMany configurations of the shared OrderLineItem CLR type —
            // see OrderReadModel below for the second, producing a distinct owned table.
            b.OwnsMany(o => o.LineItems, li =>
            {
                li.WithOwner().HasForeignKey("OrderCorrelationId");
                li.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
                li.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
            });
        });

        modelBuilder.Entity<OrderReadModel>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.Status).IsRequired().HasMaxLength(50);
            b.Property(o => o.UserId).IsRequired().HasMaxLength(200);
            b.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

            // Second independent OwnsMany configuration of the SAME OrderLineItem CLR type,
            // scoped to OrderReadModel — produces its own distinct owned table
            // (e.g. OrderReadModel_LineItem vs Order_LineItem). No second line-item class.
            b.OwnsMany(o => o.LineItems, li =>
            {
                li.WithOwner().HasForeignKey("OrderReadModelId");
                li.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
                li.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
            });
        });
    }
}
