using ECommerce.Payments.API.Data;
using ECommerce.Payments.API.Features.Payments;
using ECommerce.Payments.Commands.V1;
using ECommerce.Tests.Common;
using FluentAssertions;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ECommerce.Payments.Tests.Integration;

public class AuthorisePaymentConsumerSteps : IAsyncLifetime
{
    private readonly PostgresFixture _postgresFixture = new();
    private ServiceProvider? _provider;
    private ITestHarness? _harness;

    public async ValueTask InitializeAsync()
    {
        await _postgresFixture.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_harness is not null)
            await _harness.Stop();
        if (_provider is not null)
            await _provider.DisposeAsync();
        await _postgresFixture.DisposeAsync();
    }

    public async Task Given_HarnessWithPostgresOutbox()
    {
        var connectionString = _postgresFixture.ConnectionString;

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<PaymentsDbContext>(o =>
            o.UseNpgsql(connectionString));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<AuthorisePaymentConsumer>();
            x.AddConsumer<RefundPaymentConsumer>();

            x.AddEntityFrameworkOutbox<PaymentsDbContext>(o =>
            {
                o.UsePostgres();
            });

            x.AddConfigureEndpointsCallback((context, name, cfg) =>
            {
                cfg.UseEntityFrameworkOutbox<PaymentsDbContext>(context);
            });
        });

        _provider = services.BuildServiceProvider(true);

        // Apply migrations to create InboxState/OutboxState/ProcessedPayments tables before running tests.
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        await db.Database.MigrateAsync();

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task Given_ProcessedPaymentExists(Guid checkoutId, string outcome, decimal amount)
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        db.ProcessedPayments.Add(new ProcessedPayment
        {
            CheckoutId = checkoutId,
            Outcome = outcome,
            Amount = amount,
            FailureReason = outcome == "Failed" ? "Payment declined" : null,
            ProcessedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }

    public async Task When_AuthorisePaymentPublished(Guid checkoutId, decimal amount, bool simulateFail = false)
    {
        var message = new AuthorisePayment(
            MessageId: Guid.NewGuid(),
            CorrelationId: checkoutId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            CheckoutId: checkoutId,
            Amount: amount,
            SimulatePaymentFailure: simulateFail);

        await _harness!.Bus.Publish(message);

        await _harness!.InactivityTask;
    }

    public async Task When_AuthorisePaymentPublishedTwice(Guid checkoutId, decimal amount, bool simulateFail = false)
    {
        var message = new AuthorisePayment(
            MessageId: Guid.NewGuid(),
            CorrelationId: checkoutId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            CheckoutId: checkoutId,
            Amount: amount,
            SimulatePaymentFailure: simulateFail);

        // Two DIFFERENT transport MessageIds, SAME CheckoutId — proves business-key (not
        // transport-level) idempotency per PAY-03.
        await _harness!.Bus.Publish(message with { MessageId = Guid.NewGuid() });
        await _harness!.InactivityTask;

        await _harness!.Bus.Publish(message with { MessageId = Guid.NewGuid() });
        await _harness!.InactivityTask;
    }

    public async Task When_RefundPaymentPublished(Guid checkoutId, decimal amount, string reason)
    {
        var message = new RefundPayment(
            MessageId: Guid.NewGuid(),
            CorrelationId: checkoutId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            CheckoutId: checkoutId,
            Amount: amount,
            Reason: reason);

        await _harness!.Bus.Publish(message);

        await _harness!.InactivityTask;
    }

    public async Task Then_ExactlyOneProcessedPaymentRow(Guid checkoutId)
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        var count = await db.ProcessedPayments.CountAsync(p => p.CheckoutId == checkoutId);
        count.Should().Be(1,
            "PAY-03's business-key idempotency table should persist exactly one row per CheckoutId regardless of transport MessageId");
    }

    public async Task Then_ProcessedPaymentOutcomeIs(Guid checkoutId, string expectedOutcome)
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        var payment = await db.ProcessedPayments.FirstOrDefaultAsync(p => p.CheckoutId == checkoutId);
        payment.Should().NotBeNull();
        payment!.Outcome.Should().Be(expectedOutcome);
    }

    public async Task<int> Then_PublishedCount<T>()
        where T : class
    {
        var count = 0;
        await foreach (var _ in _harness!.Published.SelectAsync<T>())
            count++;

        return count;
    }
}
