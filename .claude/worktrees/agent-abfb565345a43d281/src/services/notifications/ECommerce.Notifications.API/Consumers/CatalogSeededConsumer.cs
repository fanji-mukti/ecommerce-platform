using ECommerce.Catalog.Events.V1;
using ECommerce.Notifications.API.Data;
using MassTransit;

namespace ECommerce.Notifications.API.Consumers;

public class CatalogSeededConsumer(NotificationsDbContext db, ILogger<CatalogSeededConsumer> logger)
    : IConsumer<CatalogSeeded>
{
    public async Task Consume(ConsumeContext<CatalogSeeded> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "CatalogSeeded received: SeedId={SeedId}, ItemCount={ItemCount}",
            msg.SeedId,
            msg.ItemCount);

        // Idempotency: MassTransit inbox deduplicates by transport MessageId.
        // This consumer body runs exactly once per unique delivery.
        // Phase 2: log-only — no real notification logic.

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
