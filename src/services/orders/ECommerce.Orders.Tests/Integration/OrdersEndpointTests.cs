using ECommerce.Tests.Common;
using FluentAssertions;
using Xunit;

namespace ECommerce.Orders.Tests.Integration;

[Collection("Integration")]
public class OrdersEndpointTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string DefaultUserId = "11111111-1111-1111-1111-111111111111";
    private const string OtherUserId = "22222222-2222-2222-2222-222222222222";

    private readonly OrdersEndpointSteps _steps = new(fixture);

    [Fact]
    public async Task TestCreateFromCart_WhenCartIsEmpty_Returns400AndNeverCallsDelete()
    {
        _steps.Given_CartStubReturnsEmptyCart();
        _steps.Given_CartStubAcceptsClear();

        var response = await _steps.When_TestCreateFromCartIsCalled(DefaultUserId);

        _steps.Then_ResponseIs400(response);
        _steps.DeleteCartCallCount().Should().Be(0);
    }

    [Fact]
    public async Task TestCreateFromCart_WhenCartHasItems_Returns202AndClearsCartExactlyOnce()
    {
        var productId = Guid.NewGuid();
        _steps.Given_CartStubReturnsCartWithItems((productId, "Widget", 9.99m, 2));
        _steps.Given_CartStubAcceptsClear();

        var response = await _steps.When_TestCreateFromCartIsCalled(DefaultUserId);

        var orderId = await _steps.Then_ResponseContainsOrderId(response);
        orderId.Should().NotBeEmpty();
        _steps.DeleteCartCallCount().Should().Be(1);
    }

    [Fact]
    public async Task GetOrderById_AfterTestCreateFromCart_EventuallyBecomesVisible()
    {
        var productId = Guid.NewGuid();
        _steps.Given_CartStubReturnsCartWithItems((productId, "Widget", 9.99m, 2));
        _steps.Given_CartStubAcceptsClear();

        var createResponse = await _steps.When_TestCreateFromCartIsCalled(DefaultUserId);
        var orderId = await _steps.Then_ResponseContainsOrderId(createResponse);

        // Proves ORD-04's eventual consistency: the read model is populated asynchronously by
        // OrderReadModelProjector after the outbox delivers OrderCreated, so GET /orders/{id}
        // may briefly 404 before succeeding.
        var (response, body) = await _steps.When_PollingUntilOrderIsVisible(DefaultUserId, orderId);

        _steps.Then_ResponseIs200(response);
        body.Should().NotBeNull();
        body!.Id.Should().Be(orderId);
        body.Status.Should().Be("Pending");
        body.LineItems.Should().ContainSingle(li => li.ProductId == productId && li.Quantity == 2);
    }

    [Fact]
    public async Task GetOrders_ForUserWithMultipleOrders_ReturnsPagedListOrderedByCreatedAtDescending()
    {
        await _steps.Given_UserHasOrders(DefaultUserId, count: 5);

        var response = await _steps.When_GetOrdersIsCalled(DefaultUserId, page: 1, pageSize: 12);

        var body = await _steps.Then_ResponseIs200WithPagedResult(response, expectedItemCount: 5, expectedTotalCount: 5);
        _steps.Then_ResponseOrdersAreOrderedByCreatedAtDescending(body);
    }

    [Fact]
    public async Task GetOrders_ForUserWithNoOrders_ReturnsEmptyItemsAndZeroCount()
    {
        // PostgresFixture's database is shared across every [Fact] in this class — establish a
        // known-empty table rather than relying on execution order relative to other tests that
        // create orders for DefaultUserId (e.g. TestCreateFromCart_*).
        await _steps.Given_NoOrdersExist();

        var response = await _steps.When_GetOrdersIsCalled(DefaultUserId);

        await _steps.Then_ResponseIs200WithPagedResult(response, expectedItemCount: 0, expectedTotalCount: 0);
    }

    [Fact]
    public async Task GetOrderById_WhenNotFound_Returns404()
    {
        var response = await _steps.When_GetOrderByIdIsCalled(DefaultUserId, Guid.NewGuid());

        _steps.Then_ResponseIs404(response);
    }

    [Fact]
    public async Task GetOrderById_WhenOwnedByDifferentUser_Returns404NotFoundNotForbidden()
    {
        var otherUsersOrderId = await _steps.Given_OrderReadModelExistsForUser(OtherUserId);

        var response = await _steps.When_GetOrderByIdIsCalled(DefaultUserId, otherUsersOrderId);

        // IDOR-safe: same 404 whether the order doesn't exist or belongs to someone else (T-03-10).
        _steps.Then_ResponseIs404(response);
    }
}
