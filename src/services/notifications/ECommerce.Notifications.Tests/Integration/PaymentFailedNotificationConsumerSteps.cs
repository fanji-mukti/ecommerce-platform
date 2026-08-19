using ECommerce.Notifications.API.Consumers;
using ECommerce.Notifications.API.Data;
using ECommerce.Payments.Events.V1;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Notifications.Tests.Integration;

public class PaymentFailedNotificationConsumerSteps : IAsyncDisposable
{
    // Unique DB name per test instance to avoid cross-test row bleed.
    private readonly string _dbName = $"notifications-payment-failed-{Guid.NewGuid()}";
    private ServiceProvider? _provider;
    private ITestHarness? _harness;

    public async Task Given_HarnessWithInMemoryTransport()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<NotificationsDbContext>(o => o.UseInMemoryDatabase(_dbName));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<PaymentFailedNotificationConsumer>();
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task When_PaymentFailedPublished(string userId, Guid checkoutId, decimal amount, string reason, DateTimeOffset failedAt)
    {
        var messageId = Guid.NewGuid();
        var message = new PaymentFailed(
            MessageId: messageId,
            CorrelationId: checkoutId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            CheckoutId: checkoutId,
            UserId: userId,
            Amount: amount,
            Reason: reason,
            FailedAt: failedAt);

        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
        await _harness!.InactivityTask;
    }

    public async Task Then_NotificationEntryExists(string expectedMessage, string expectedEventType)
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        var entries = await db.NotificationEntries.ToListAsync();
        entries.Should().ContainSingle(n => n.Message == expectedMessage && n.EventType == expectedEventType,
            "PaymentFailedNotificationConsumer should insert exactly one correctly-populated row");
    }

    public async ValueTask DisposeAsync()
    {
        if (_harness is not null)
            await _harness.Stop();
        if (_provider is not null)
            await _provider.DisposeAsync();
    }
}
