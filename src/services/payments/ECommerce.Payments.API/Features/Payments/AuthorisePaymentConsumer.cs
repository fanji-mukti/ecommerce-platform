using ECommerce.Payments.API.Data;
using ECommerce.Payments.Commands.V1;
using ECommerce.Payments.Events.V1;
using MassTransit;

namespace ECommerce.Payments.API.Features.Payments;

public class AuthorisePaymentConsumer(PaymentsDbContext db, IPublishEndpoint publish)
    : IConsumer<AuthorisePayment>
{
    private const string DeclinedReason = "Payment declined";

    public async Task Consume(ConsumeContext<AuthorisePayment> context)
    {
        var msg = context.Message;
        var now = DateTimeOffset.UtcNow;

        var existing = await db.ProcessedPayments.FindAsync([msg.CheckoutId], context.CancellationToken);
        if (existing is not null)
        {
            // PAY-03: replay the STORED outcome — never recompute or re-insert on redelivery.
            if (existing.Outcome == "Authorised")
            {
                await publish.Publish(new PaymentAuthorised(
                    Guid.NewGuid(), msg.CheckoutId, msg.MessageId, now,
                    msg.CheckoutId, existing.Amount, existing.ProcessedAt), context.CancellationToken);
            }
            else
            {
                await publish.Publish(new PaymentFailed(
                    Guid.NewGuid(), msg.CheckoutId, msg.MessageId, now,
                    msg.CheckoutId, existing.Amount, existing.FailureReason!, existing.ProcessedAt), context.CancellationToken);
            }

            return;
        }

        // PAY-02: deterministic failure rule — amount ends in .99 OR the demo toggle is set.
        var cents = decimal.Round((msg.Amount % 1m) * 100m);
        var endsIn99 = cents == 99m;
        var shouldFail = msg.SimulatePaymentFailure || endsIn99;

        if (shouldFail)
        {
            db.ProcessedPayments.Add(new ProcessedPayment
            {
                CheckoutId = msg.CheckoutId,
                Outcome = "Failed",
                Amount = msg.Amount,
                FailureReason = DeclinedReason,
                ProcessedAt = now
            });

            await publish.Publish(new PaymentFailed(
                Guid.NewGuid(), msg.CheckoutId, msg.MessageId, now,
                msg.CheckoutId, msg.Amount, DeclinedReason, now), context.CancellationToken);
        }
        else
        {
            db.ProcessedPayments.Add(new ProcessedPayment
            {
                CheckoutId = msg.CheckoutId,
                Outcome = "Authorised",
                Amount = msg.Amount,
                FailureReason = null,
                ProcessedAt = now
            });

            await publish.Publish(new PaymentAuthorised(
                Guid.NewGuid(), msg.CheckoutId, msg.MessageId, now,
                msg.CheckoutId, msg.Amount, now), context.CancellationToken);
        }

        // Publish BEFORE SaveChangesAsync so the transactional outbox commits message + row atomically.
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
