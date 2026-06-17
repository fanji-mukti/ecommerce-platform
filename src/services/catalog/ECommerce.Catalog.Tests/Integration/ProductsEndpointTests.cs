using ECommerce.Tests.Common;
using Xunit;

namespace ECommerce.Catalog.Tests.Integration;

[Collection("Integration")]
public class ProductsEndpointTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly ProductsEndpointSteps _steps = new(fixture);

    [Fact]
    public async Task GetProducts_WhenCatalogHasProducts_ReturnsPagedList()
    {
        await _steps.Given_CatalogHasProducts(count: 15);
        var response = await _steps.When_GetProductsIsCalled(page: 1, pageSize: 12);
        await _steps.Then_ResponseIs200WithPagedResult(response, expectedItemCount: 12, expectedTotalCount: 15);
    }

    [Fact]
    public async Task GetProducts_WhenCategoryFilterApplied_ReturnsFilteredProducts()
    {
        await _steps.Given_CatalogHasProductsInCategory("Electronics", count: 5, otherCount: 10);
        var response = await _steps.When_GetProductsIsCalled(page: 1, pageSize: 12, category: "Electronics");
        await _steps.Then_ResponseIs200WithPagedResult(response, expectedItemCount: 5, expectedTotalCount: 5);
    }

    [Fact]
    public async Task GetProductById_WhenExists_Returns200()
    {
        var knownId = Guid.NewGuid();
        await _steps.Given_ProductWithId(knownId, name: "Known Product", sku: "KNW-001");
        var response = await _steps.When_GetProductByIdIsCalled(knownId);
        await _steps.Then_ResponseContainsProductId(response, knownId);
    }

    [Fact]
    public async Task GetProductById_WhenNotFound_Returns404()
    {
        var unknownId = Guid.NewGuid();
        var response = await _steps.When_GetProductByIdIsCalled(unknownId);
        _steps.Then_ResponseIs404(response);
    }

    [Fact]
    public async Task GetProductById_WhenNotFound_ReturnsErrorMessage()
    {
        var unknownId = Guid.NewGuid();
        var response = await _steps.When_GetProductByIdIsCalled(unknownId);
        await _steps.Then_ResponseContainsErrorMessage(response, "Product not found.");
    }
}
