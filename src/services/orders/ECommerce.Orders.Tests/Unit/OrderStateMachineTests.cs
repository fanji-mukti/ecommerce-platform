using ECommerce.Payments.Commands.V1;
using ECommerce.Payments.Events.V1;
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

    [Fact]
    public async Task PaymentAuthorised_WhenPending_TransitionsToPaid()
    {
        var orderId = Guid.NewGuid();

        await _steps.Given_OrderCreatedPublished(orderId);
        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Pending);

        await _steps.Given_PaymentAuthorisedPublished(orderId);

        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Paid);
    }

    [Fact]
    public async Task PaymentFailed_WhenPending_TransitionsToCancelledWithFailureReason()
    {
        var orderId = Guid.NewGuid();

        await _steps.Given_OrderCreatedPublished(orderId);
        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Pending);

        // CHK-03: PaymentFailed deterministically cancels the order and records a
        // human-readable failure reason on the saga instance.
        await _steps.Given_PaymentFailedPublished(orderId, reason: "Payment declined");

        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Cancelled);
        await _steps.Then_SagaHasFailureReason(orderId, "Payment declined");
    }

    [Fact]
    public async Task FulfillmentFailed_WhenPaid_TransitionsToCancelledAndPublishesRefundPayment()
    {
        var orderId = Guid.NewGuid();

        await _steps.Given_OrderCreatedPublished(orderId);
        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Pending);

        await _steps.Given_PaymentAuthorisedPublished(orderId);
        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Paid);

        // CHK-04: FulfillmentFailed triggers a RefundPayment publish and cancels the order
        // in one step (Open Question 2 resolved — no intermediate "Refunding" state).
        await _steps.When_FulfillmentFailedPublished(orderId, reason: "Warehouse out of stock");

        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Cancelled);
        await _steps.Then_SagaHasFailureReason(orderId, "Fulfillment failed — order cancelled and refunded (Warehouse out of stock)");
        await _steps.Then_MessagePublished<RefundPayment>(msg => msg.CheckoutId == orderId);
    }

    [Fact]
    public async Task PaymentAuthorised_WhenAlreadyCancelled_IsAbsorbedWithoutFault()
    {
        var orderId = Guid.NewGuid();

        await _steps.Given_OrderCreatedPublished(orderId);
        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Pending);

        await _steps.Given_PaymentFailedPublished(orderId, reason: "Payment declined");
        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Cancelled);

        // Late/redelivered PaymentAuthorised arriving after the saga already reached the
        // terminal Cancelled state (CR-01) — must be absorbed, not faulted.
        await _steps.Given_PaymentAuthorisedPublished(orderId);

        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Cancelled);
        await _steps.Then_NoFaultPublished<PaymentAuthorised>();
    }

    [Fact]
    public async Task CheckoutTimeoutExpired_WhenPaymentOutcomeNeverArrives_TransitionsToCancelledWithFailureReason()
    {
        var orderId = Guid.NewGuid();

        // CHK-05 / D-05: CheckoutOptions.TimeoutMinutes is configured to 0.05 (3 seconds) in
        // OrderStateMachineSteps.InitializeAsync — this test genuinely waits a few real
        // seconds for the scheduled timeout to fire, proving the same compensation path as
        // an explicit PaymentFailed fires correctly when no payment outcome ever arrives.
        await _steps.Given_OrderCreatedPublished(orderId);
        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Pending);

        await _steps.When_CheckoutTimeoutExpires(orderId);

        await _steps.Then_SagaExistsInState(orderId, _steps.Machine.Cancelled);
        await _steps.Then_SagaHasFailureReason(orderId, "Checkout timed out before payment completed");
    }
}
