using Xunit;

namespace ECommerce.Orders.Tests.Unit;

public class OrderStateMachineTests : IAsyncLifetime
{
    private readonly OrderStateMachineSteps _steps = new();

    public ValueTask InitializeAsync() => _steps.InitializeAsync();

    public ValueTask DisposeAsync() => _steps.DisposeAsync();

    [Fact]
    public async Task OrderStatusChanged_WhenPendingSkipsToFulfilled_TransitionIsRejected()
    {
        var orderId = Guid.NewGuid();

        await _steps.Given_OrderCreatedPublished(orderId);
        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Pending);

        // Invalid transition: Pending -> Fulfilled directly, skipping Paid (ORD-03).
        await _steps.When_OrderStatusChangedPublished(orderId, previousStatus: "Pending", newStatus: "Fulfilled");

        // The catch-all combinator absorbs this event; CurrentState remains Pending.
        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Pending);
    }

    [Fact]
    public async Task OrderStatusChanged_WhenPendingToPaid_TransitionSucceeds()
    {
        var orderId = Guid.NewGuid();

        await _steps.Given_OrderCreatedPublished(orderId);
        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Pending);

        await _steps.When_OrderStatusChangedPublished(orderId, previousStatus: "Pending", newStatus: "Paid");

        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Paid);
    }
}
