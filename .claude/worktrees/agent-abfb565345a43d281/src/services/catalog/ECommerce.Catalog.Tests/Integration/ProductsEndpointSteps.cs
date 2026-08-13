using System.Net;
using System.Net.Http.Json;
using ECommerce.Catalog.API.Data;
using ECommerce.Catalog.API.Features.Products;
using ECommerce.Tests.Common;
using ECommerce.Tests.Common.Builders;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.Catalog.Tests.Integration;

/// <summary>
/// WebApplicationFactory for Catalog integration tests.
/// Swaps the postgres connection string and replaces Azure Service Bus with in-memory transport.
/// </summary>
internal sealed class CatalogWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:postgres"] = connectionString,
                // Provide a placeholder so MassTransit config doesn't throw on null host
                ["ConnectionStrings:messaging"] = "placeholder"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove DbInitializer so it does not run during test host startup.
            // DbInitializer migrates the DB and also seeds products via the EF outbox,
            // which can conflict with the test helpers that clear and seed the DB.
            services.RemoveAll<ECommerce.Catalog.API.Data.DbInitializer>();
        });
    }
}

public record PagedResult<T>(T[] Items, int TotalCount, int Page, int PageSize);

public class ProductsEndpointSteps(PostgresFixture fixture)
{
    private readonly CatalogWebApplicationFactory _factory = new(fixture.ConnectionString);
    private HttpClient? _client;

    private HttpClient Client => _client ??= _factory.CreateClient();

    private async Task<CatalogDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        var db = new CatalogDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }

    public async Task Given_CatalogHasProducts(int count)
    {
        await using var db = await CreateDbContextAsync();
        // Clear any existing products for test isolation
        db.Products.RemoveRange(db.Products);
        await db.SaveChangesAsync();

        var builder = new ProductBuilder();
        var products = Enumerable.Range(1, count)
            .Select(i =>
            {
                var data = builder.WithName($"Product {i:D3}").WithSku($"TST-{i:D3}").Build();
                return new Product
                {
                    Id = data.Id,
                    Name = data.Name,
                    Sku = data.Sku,
                    Description = data.Description,
                    Price = data.Price,
                    StockQuantity = data.StockQuantity,
                    Category = data.Category,
                    ImageUrl = data.ImageUrl,
                    CreatedAt = data.CreatedAt
                };
            })
            .ToList();

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }

    public async Task Given_CatalogHasProductsInCategory(string category, int count, string otherCategory = "Books", int otherCount = 10)
    {
        await using var db = await CreateDbContextAsync();
        // Clear existing products for test isolation
        db.Products.RemoveRange(db.Products);
        await db.SaveChangesAsync();

        var products = new List<Product>();

        // Add products in the target category
        for (int i = 1; i <= count; i++)
        {
            products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = $"{category} Product {i:D3}",
                Sku = $"{category.Substring(0, 3).ToUpper()}-{i:D3}",
                Description = "A test product.",
                Price = 9.99m,
                StockQuantity = 100,
                Category = category,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Add products in another category to ensure filtering works
        for (int i = 1; i <= otherCount; i++)
        {
            products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = $"{otherCategory} Product {i:D3}",
                Sku = $"{otherCategory.Substring(0, 3).ToUpper()}-{i:D3}",
                Description = "Another test product.",
                Price = 14.99m,
                StockQuantity = 50,
                Category = otherCategory,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }

    public async Task Given_ProductWithId(Guid id, string name = "Known Product", string sku = "KNW-001")
    {
        await using var db = await CreateDbContextAsync();
        db.Products.Add(new Product
        {
            Id = id,
            Name = name,
            Sku = sku,
            Description = "A known product.",
            Price = 19.99m,
            StockQuantity = 10,
            Category = "Electronics",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task<HttpResponseMessage> When_GetProductsIsCalled(
        int page = 1, int pageSize = 12, string? category = null)
    {
        var url = $"/products?page={page}&pageSize={pageSize}";
        if (category is not null) url += $"&category={Uri.EscapeDataString(category)}";
        return await Client.GetAsync(url);
    }

    public async Task<HttpResponseMessage> When_GetProductByIdIsCalled(Guid id)
        => await Client.GetAsync($"/products/{id}");

    public async Task Then_ResponseIs200WithPagedResult(
        HttpResponseMessage response, int expectedItemCount, int expectedTotalCount)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(expectedItemCount);
        body.TotalCount.Should().Be(expectedTotalCount);
    }

    public void Then_ResponseIs200(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.OK);

    public void Then_ResponseIs404(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    public async Task Then_ResponseContainsProductId(HttpResponseMessage response, Guid expectedId)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProductDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(expectedId);
    }

    public async Task Then_ResponseContainsErrorMessage(HttpResponseMessage response, string expectedError)
    {
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(expectedError);
    }
}
