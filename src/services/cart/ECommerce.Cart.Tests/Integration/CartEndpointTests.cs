using ECommerce.Tests.Common;
using FluentAssertions;
using Xunit;

namespace ECommerce.Cart.Tests.Integration;

[Collection("Integration")]
public class CartEndpointTests(RedisFixture fixture) : IClassFixture<RedisFixture>, IDisposable
{
    private readonly CartEndpointSteps _steps = new(fixture);

    [Fact]
    public async Task AddItem_WhenProductNotInCart_CallsCatalogOnceAndStoresSnapshot()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();
        _steps.Given_CatalogHasProduct(productId, "Widget", 19.99m);

        var response = await _steps.When_AddItemIsCalled(userId, productId, quantity: 2);

        var cart = await _steps.Then_ResponseContainsCart(response);
        cart.Items.Should().ContainSingle(i => i.ProductId == productId && i.ProductName == "Widget" && i.UnitPrice == 19.99m && i.Quantity == 2);
        _steps.CatalogCallCountFor(productId).Should().Be(1);
    }

    [Fact]
    public async Task AddItem_WhenProductAlreadyInCart_IncrementsQuantityWithoutRecallingCatalog()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();
        _steps.Given_CatalogHasProduct(productId, "Widget", 19.99m);
        await _steps.When_AddItemIsCalled(userId, productId, quantity: 2);

        var response = await _steps.When_AddItemIsCalled(userId, productId, quantity: 3);

        var cart = await _steps.Then_ResponseContainsCart(response);
        cart.Items.Should().ContainSingle(i => i.ProductId == productId && i.ProductName == "Widget" && i.UnitPrice == 19.99m && i.Quantity == 5);
        _steps.CatalogCallCountFor(productId).Should().Be(1); // no re-fetch on repeat add (CART-01/02)
    }

    [Fact]
    public async Task PatchQuantity_SetsAbsoluteValue()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();
        _steps.Given_CatalogHasProduct(productId, "Widget", 19.99m);
        await _steps.When_AddItemIsCalled(userId, productId, quantity: 2);

        var response = await _steps.When_UpdateQuantityIsCalled(userId, productId, quantity: 9);

        var cart = await _steps.Then_ResponseContainsCart(response);
        cart.Items.Should().ContainSingle(i => i.ProductId == productId && i.Quantity == 9);
    }

    [Fact]
    public async Task PatchQuantity_LessThanOne_Returns400()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();
        _steps.Given_CatalogHasProduct(productId, "Widget", 19.99m);
        await _steps.When_AddItemIsCalled(userId, productId, quantity: 2);

        var response = await _steps.When_UpdateQuantityIsCalled(userId, productId, quantity: 0);

        _steps.Then_ResponseIs400(response);
    }

    [Fact]
    public async Task DeleteItem_WhenPresent_Returns200WithUpdatedCart()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();
        _steps.Given_CatalogHasProduct(productId, "Widget", 19.99m);
        await _steps.When_AddItemIsCalled(userId, productId, quantity: 2);

        var response = await _steps.When_DeleteItemIsCalled(userId, productId);

        var cart = await _steps.Then_ResponseContainsCart(response);
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteItem_WhenNotInCart_Returns404()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();

        var response = await _steps.When_DeleteItemIsCalled(userId, productId);

        _steps.Then_ResponseIs404(response);
    }

    [Fact]
    public async Task GetCart_WhenEmpty_ReturnsEmptyItemsAndZeroGrandTotal()
    {
        var userId = Guid.NewGuid().ToString();

        var response = await _steps.When_GetCartIsCalled(userId);

        var cart = await _steps.Then_ResponseContainsCart(response);
        cart.Items.Should().BeEmpty();
        cart.ItemCount.Should().Be(0);
        cart.GrandTotal.Should().Be(0);
    }

    [Fact]
    public async Task GetCart_WithItems_ReturnsCorrectLineAndGrandTotals()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();
        _steps.Given_CatalogHasProduct(productId, "Widget", 19.99m);
        await _steps.When_AddItemIsCalled(userId, productId, quantity: 3);

        var response = await _steps.When_GetCartIsCalled(userId);

        var cart = await _steps.Then_ResponseContainsCart(response);
        cart.Items.Should().ContainSingle(i => i.LineTotal == 19.99m * 3);
        cart.GrandTotal.Should().Be(19.99m * 3);
    }

    [Fact]
    public async Task DeleteCart_ClearsAllItems()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();
        _steps.Given_CatalogHasProduct(productId, "Widget", 19.99m);
        await _steps.When_AddItemIsCalled(userId, productId, quantity: 2);

        var clearResponse = await _steps.When_ClearCartIsCalled(userId);
        _steps.Then_ResponseIs204(clearResponse);

        var getResponse = await _steps.When_GetCartIsCalled(userId);
        var cart = await _steps.Then_ResponseContainsCart(getResponse);
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCart_WithoutBearerToken_Returns401()
    {
        var response = await _steps.When_GetCartIsCalledWithoutAuth();

        _steps.Then_ResponseIs401(response);
    }

    public void Dispose() => _steps.Dispose();
}
