using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Fulfillment.API.Data;

public class FulfillmentDbContext(DbContextOptions<FulfillmentDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit inbox/outbox tables required for idempotent inbox pattern
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
