using FluentAssertions;
using Xunit;

namespace ECommerce.Checkout.Tests.Integration;

public class CheckoutEndpointTests : IAsyncLifetime
{
    private const string DefaultUserId = "11111111-1111-1111-1111-111111111111";

    private readonly CheckoutEndpointSteps _steps = new();

    public ValueTask InitializeAsync() => _steps.InitializeAsync();

    public ValueTask DisposeAsync() => _steps.DisposeAsync();

    [Fact]
    public async Task Checkout_WhenCartHasItems_Returns202WithCheckoutId()
    {
        _steps.Given_OrdersStubAcceptsCheckout(Guid.NewGuid());

        var response = await _steps.When_CheckoutIsCalled(DefaultUserId);

        var checkoutId = await _steps.Then_ResponseContainsCheckoutId(response);
        checkoutId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Checkout_WhenCartIsEmpty_Returns400()
    {
        _steps.Given_OrdersStubRejectsEmptyCart();

        var response = await _steps.When_CheckoutIsCalled(DefaultUserId);

        _steps.Then_ResponseIs400(response);
    }

    [Fact]
    public async Task GetCheckoutStatus_WhenOrdersHasNotMaterializedYet_ReturnsStarted()
    {
        var checkoutId = Guid.NewGuid();
        _steps.Given_OrdersStubReturns404ForStatus(checkoutId);

        var response = await _steps.When_GetCheckoutStatusIsCalled(DefaultUserId, checkoutId);

        var body = await _steps.Then_ResponseIs200(response);
        _steps.Then_StatusIs(body, "Started");
    }

    [Fact]
    public async Task GetCheckoutStatus_WhenOrdersReturnsPending_MapsToAwaitingPayment()
    {
        var checkoutId = Guid.NewGuid();
        _steps.Given_OrdersStubReturnsStatus(checkoutId, "Pending", failureReason: null);

        var response = await _steps.When_GetCheckoutStatusIsCalled(DefaultUserId, checkoutId);

        var body = await _steps.Then_ResponseIs200(response);
        _steps.Then_StatusIs(body, "AwaitingPayment");
    }

    [Fact]
    public async Task GetCheckoutStatus_WhenOrdersReturnsCancelledWithFailureReason_SurfacesReasonUnchanged()
    {
        var checkoutId = Guid.NewGuid();
        _steps.Given_OrdersStubReturnsStatus(checkoutId, "Cancelled", failureReason: "Payment declined");

        var response = await _steps.When_GetCheckoutStatusIsCalled(DefaultUserId, checkoutId);

        var body = await _steps.Then_ResponseIs200(response);
        _steps.Then_StatusIs(body, "Cancelled");
        body!.FailureReason.Should().Be("Payment declined");
    }

    [Fact]
    public async Task SimulateFulfillmentFailure_WhenOrderIsOwnedAndPaid_Returns202()
    {
        var checkoutId = Guid.NewGuid();
        _steps.Given_OrdersStubReturnsStatus(checkoutId, "Paid", failureReason: null);

        var response = await _steps.When_SimulateFulfillmentFailureIsCalled(DefaultUserId, checkoutId);

        _steps.Then_ResponseIs202(response);
    }

    [Fact]
    public async Task SimulateFulfillmentFailure_WhenOrderNotFoundOrNotOwned_Returns404()
    {
        var checkoutId = Guid.NewGuid();
        _steps.Given_OrdersStubReturns404ForStatus(checkoutId);

        var response = await _steps.When_SimulateFulfillmentFailureIsCalled(DefaultUserId, checkoutId);

        _steps.Then_ResponseIs404(response);
    }
}
