using ECommerce.Payments.API.Features.Payments;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payments.API.Data;

public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
    : DbContext(options)
{
    public DbSet<ProcessedPayment> ProcessedPayments => Set<ProcessedPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit outbox/inbox tables — required for transactional outbox
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<ProcessedPayment>(b =>
        {
            b.HasKey(p => p.CheckoutId);
            b.Property(p => p.Outcome).IsRequired().HasMaxLength(20);
            b.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            b.Property(p => p.FailureReason).HasMaxLength(500);
            b.HasIndex(p => p.CheckoutId).IsUnique();
        });
    }
}
