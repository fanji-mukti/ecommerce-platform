using ECommerce.Fulfillment.Events.V1;
using ECommerce.Orders.API.Features.Orders;
using ECommerce.Orders.Events.V1;
using ECommerce.Payments.Events.V1;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ECommerce.Orders.Tests.Unit;

/// <summary>
/// Drives OrderStateMachine transitions through MassTransit's in-memory saga test harness
/// (no live Postgres/ASB required — MassTransit.TestFramework's InMemoryRepository).
/// </summary>
public class OrderStateMachineSteps : IAsyncLifetime
{
    private ServiceProvider? _provider;
    private ITestHarness? _harness;
    private ISagaStateMachineTestHarness<OrderStateMachine, Order>? _sagaHarness;

    public OrderStateMachine Machine { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // 0.05 minutes = 3 seconds — short enough for the CHK-05 real-time timeout test
        // (D-05) to genuinely wait a few seconds rather than the production 15-minute default.
        services.Configure<CheckoutOptions>(o => o.TimeoutMinutes = 0.05);

        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<OrderStateMachine, Order>()
                .InMemoryRepository();

            // A message scheduler must be explicitly attached for OrderStateMachine's
            // .Schedule()/.Unschedule() (CHK-05) to work — the harness's implicit in-memory
            // bus has no scheduler configured by default.
            x.UsingInMemory((context, cfg) =>
            {
                cfg.UseInMemoryScheduler();
                cfg.ConfigureEndpoints(context);
            });
        });

        _provider = services.BuildServiceProvider(true);
        Machine = _provider.GetRequiredService<OrderStateMachine>();

        _harness = _provider.GetRequiredService<ITestHarness>();
        // Default TestTimeout/TestInactivityTimeout are too short for the CHK-05 timeout
        // test's genuine ~3s scheduled-message wait (D-05) — widen them so that test isn't
        // flaky against the default.
        _harness.TestTimeout = TimeSpan.FromSeconds(20);
        _harness.TestInactivityTimeout = TimeSpan.FromSeconds(5);
        await _harness.Start();

        _sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, Order>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_harness is not null)
            await _harness.Stop();
        if (_provider is not null)
            await _provider.DisposeAsync();
    }

    public async Task Given_OrderCreatedPublished(Guid orderId)
    {
        await _harness!.Bus.Publish(new OrderCreated(
            MessageId: Guid.NewGuid(),
            CorrelationId: orderId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            OrderId: orderId,
            UserId: "user-1",
            LineItems: [new OrderLineItemData(Guid.NewGuid(), "Test Product", 19.99m, 2)],
            TotalAmount: 39.98m,
            CreatedAt: DateTimeOffset.UtcNow));

        (await _harness!.Consumed.Any<OrderCreated>(x => x.Context.Message.OrderId == orderId))
            .Should().BeTrue("the saga should have consumed the OrderCreated event before proceeding");
    }

    public async Task When_OrderStatusChangedPublished(Guid orderId, string previousStatus, string newStatus)
    {
        await _harness!.Bus.Publish(new OrderStatusChanged(
            MessageId: Guid.NewGuid(),
            CorrelationId: orderId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            OrderId: orderId,
            PreviousStatus: previousStatus,
            NewStatus: newStatus,
            ChangedAt: DateTimeOffset.UtcNow));

        (await _harness!.Consumed.Any<OrderStatusChanged>(x => x.Context.Message.OrderId == orderId && x.Context.Message.NewStatus == newStatus))
            .Should().BeTrue("the saga should have consumed the OrderStatusChanged event before proceeding");
    }

    public async Task Given_PaymentAuthorisedPublished(Guid orderId)
    {
        await _harness!.Bus.Publish(new PaymentAuthorised(
            MessageId: Guid.NewGuid(),
            CorrelationId: orderId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            CheckoutId: orderId,
            Amount: 39.98m,
            AuthorisedAt: DateTimeOffset.UtcNow));

        (await _harness!.Consumed.Any<PaymentAuthorised>(x => x.Context.Message.CheckoutId == orderId))
            .Should().BeTrue("the saga should have consumed the PaymentAuthorised event before proceeding");
    }

    public async Task Given_PaymentFailedPublished(Guid orderId, string reason)
    {
        await _harness!.Bus.Publish(new PaymentFailed(
            MessageId: Guid.NewGuid(),
            CorrelationId: orderId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            CheckoutId: orderId,
            Amount: 39.98m,
            Reason: reason,
            FailedAt: DateTimeOffset.UtcNow));

        (await _harness!.Consumed.Any<PaymentFailed>(x => x.Context.Message.CheckoutId == orderId))
            .Should().BeTrue("the saga should have consumed the PaymentFailed event before proceeding");
    }

    public async Task When_FulfillmentFailedPublished(Guid orderId, string reason)
    {
        await _harness!.Bus.Publish(new FulfillmentFailed(
            MessageId: Guid.NewGuid(),
            CorrelationId: orderId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            CheckoutId: orderId,
            Reason: reason,
            FailedAt: DateTimeOffset.UtcNow));

        (await _harness!.Consumed.Any<FulfillmentFailed>(x => x.Context.Message.CheckoutId == orderId))
            .Should().BeTrue("the saga should have consumed the FulfillmentFailed event before proceeding");
    }

    public async Task When_CheckoutTimeoutExpires(Guid orderId)
    {
        // CHK-05 (D-05): genuinely waits for the scheduled CheckoutTimeoutExpired event to
        // fire via the in-memory Quartz scheduler (CheckoutOptions.TimeoutMinutes = 0.05 = 3s,
        // see InitializeAsync) — real-time wait over unverified time-travel APIs, per
        // RESEARCH.md Pattern 2's recommendation.
        //
        // Polls saga state directly rather than harness.Consumed.Any<CheckoutTimeoutExpired>():
        // empirically (verified via a standalone repro against MassTransit.Quartz 8.3.6's
        // in-memory scheduler), the scheduler's re-publish path is NOT observed by the test
        // harness's Consumed tracker, even though the saga itself genuinely receives and
        // processes the message — confirmed by CurrentState transitioning out of Pending.
        // Consumed.Any<CheckoutTimeoutExpired>() reliably returns false here regardless of
        // wait duration, so it cannot be used as the wait condition.
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var current = _sagaHarness!.Created.Contains(orderId);
            if (current is not null && current.CurrentState != Machine.Pending.Name)
                return;
            await Task.Delay(250);
        }

        throw new TimeoutException(
            $"Saga {orderId} did not leave {Machine.Pending.Name} within 15 seconds — the scheduled CheckoutTimeoutExpired event never fired.");
    }

    public Task Then_SagaExistsInState(Guid orderId, State expectedState)
    {
        // Synchronous by design (MassTransit.Testing.StateMachineSagaTestHarnessExtensions) —
        // safe here because Given_/When_ already awaited harness.Consumed.Any<T>() for the
        // triggering event, so the in-memory saga repository is already up to date.
        var found = _sagaHarness!.Created.ContainsInState(orderId, Machine, expectedState);
        found.Should().NotBeNull($"the saga instance {orderId} should exist and be in state {expectedState.Name}");
        found!.CurrentState.Should().Be(expectedState.Name);
        return Task.CompletedTask;
    }

    public Task Then_SagaHasFailureReason(Guid orderId, string expectedReason)
    {
        var found = _sagaHarness!.Created.Contains(orderId);
        found.Should().NotBeNull($"the saga instance {orderId} should exist");
        found!.FailureReason.Should().Be(expectedReason);
        return Task.CompletedTask;
    }

    public async Task Then_MessagePublished<T>(Func<T, bool> predicate) where T : class
    {
        (await _harness!.Published.Any<T>(ctx => predicate(ctx.Context.Message)))
            .Should().BeTrue($"a matching {typeof(T).Name} message should have been published");
    }
}
