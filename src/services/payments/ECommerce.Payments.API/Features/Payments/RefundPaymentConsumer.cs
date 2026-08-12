using ECommerce.Payments.API.Data;
using ECommerce.Payments.Commands.V1;
using ECommerce.Payments.Events.V1;
using MassTransit;

namespace ECommerce.Payments.API.Features.Payments;

public class RefundPaymentConsumer(PaymentsDbContext db, IPublishEndpoint publish)
    : IConsumer<RefundPayment>
{
    public async Task Consume(ConsumeContext<RefundPayment> context)
    {
        var msg = context.Message;
        var now = DateTimeOffset.UtcNow;

        var existing = await db.ProcessedPayments.FindAsync([msg.CheckoutId], context.CancellationToken);
        if (existing is null || existing.Outcome != "Authorised")
        {
            // Idempotent no-op: nothing to refund, already refunded (T-04-10), or the
            // payment was never authorised in the first place (WR-03 — defense-in-depth
            // against refunding money that was never taken; the saga cannot currently reach
            // this path in normal flow, but the consumer itself must guard the precondition).
            return;
        }

        existing.Outcome = "Refunded";

        await publish.Publish(new PaymentRefunded(
            Guid.NewGuid(), msg.CheckoutId, msg.MessageId, now,
            msg.CheckoutId, existing.Amount, now), context.CancellationToken);

        // Publish BEFORE SaveChangesAsync so the transactional outbox commits message + row atomically.
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
