using ECommerce.Notifications.API.Features.Notifications;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Notifications.API.Data;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options)
{
    public DbSet<NotificationEntry> NotificationEntries => Set<NotificationEntry>();
    public DbSet<OrderStatusSnapshot> OrderStatusSnapshots => Set<OrderStatusSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit inbox/outbox tables required for idempotent inbox pattern
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<NotificationEntry>(b =>
        {
            b.HasKey(n => n.Id);
            b.Property(n => n.UserId).IsRequired().HasMaxLength(100);
            b.Property(n => n.Message).IsRequired().HasMaxLength(500);
            b.Property(n => n.EventType).IsRequired().HasMaxLength(50);
            b.HasIndex(n => n.UserId);
        });

        modelBuilder.Entity<OrderStatusSnapshot>(b =>
        {
            b.HasKey(s => s.OrderId);
            b.Property(s => s.Status).IsRequired().HasMaxLength(20);
        });
    }
}
