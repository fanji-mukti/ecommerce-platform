using ECommerce.Orders.API.Features.Orders;
using ECommerce.Orders.Events.V1;
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

        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<OrderStateMachine, Order>()
                .InMemoryRepository();
        });

        _provider = services.BuildServiceProvider(true);
        Machine = _provider.GetRequiredService<OrderStateMachine>();

        _harness = _provider.GetRequiredService<ITestHarness>();
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
}
