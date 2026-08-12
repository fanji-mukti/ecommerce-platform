using System.Net;
using System.Net.Http.Json;
using ECommerce.Orders.API.Data;
using ECommerce.Orders.API.Features.Orders;
using ECommerce.Tests.Common;
using ECommerce.Tests.Common.Builders;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ECommerce.Orders.Tests.Integration;

/// <summary>
/// WebApplicationFactory for Orders integration tests.
/// Swaps the postgres connection string, forces MassTransit onto the in-memory transport
/// (Program.cs's "placeholder" sentinel), points the typed Cart HTTP client at a WireMock
/// stub, and replaces the real JwtBearer scheme with the shared TestAuthHandler.
/// </summary>
internal sealed class OrdersWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _postgresConnectionString;
    private readonly string _cartBaseAddress;

    public OrdersWebApplicationFactory(string postgresConnectionString, string cartBaseAddress)
    {
        _postgresConnectionString = postgresConnectionString;
        _cartBaseAddress = cartBaseAddress;

        // Aspire's AddNpgsqlDbContext (like Aspire.StackExchange.Redis's AddRedisClient — see
        // CartWebApplicationFactory) reads ConnectionStrings:postgres EAGERLY, before
        // WebApplicationFactory's ConfigureAppConfiguration override can reach it. Setting the
        // environment variable here, before the host is constructed, ensures
        // WebApplication.CreateBuilder's built-in AddEnvironmentVariables() source has the test
        // connection string in place before Program.cs's own code runs. The same eagerness
        // applies to the "messaging" connection string read inside the AddMassTransit(...) call.
        Environment.SetEnvironmentVariable("ConnectionStrings__postgres", postgresConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__messaging", "placeholder");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var postgresConnectionString = _postgresConnectionString;
        var cartBaseAddress = _cartBaseAddress;

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:postgres"] = postgresConnectionString,
                // Provide a placeholder so MassTransit's in-memory-transport branch activates.
                ["ConnectionStrings:messaging"] = "placeholder"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove DbInitializer so it does not race with test-driven migrations/seeding.
            services.RemoveAll<DbInitializer>();

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Point the typed Cart HTTP client at the WireMock stub instead of http://cart.
            services.AddHttpClient<ICartClient, CartClient>(c => c.BaseAddress = new Uri(cartBaseAddress));
        });
    }
}

public record PagedResult<T>(T[] Items, int TotalCount, int Page, int PageSize);

public class OrdersEndpointSteps : IDisposable
{
    private readonly string _postgresConnectionString;
    private readonly WireMockServer _cartStub = WireMockServer.Start();
    private readonly OrdersWebApplicationFactory _factory;
    private HttpClient? _client;

    public OrdersEndpointSteps(PostgresFixture fixture)
    {
        _postgresConnectionString = fixture.ConnectionString;
        _factory = new OrdersWebApplicationFactory(fixture.ConnectionString, _cartStub.Urls[0]);
    }

    private HttpClient Client => _client ??= _factory.CreateClient();

    private async Task<OrdersDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseNpgsql(_postgresConnectionString)
            .Options;
        var db = new OrdersDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }

    // ----- Given -----

    public void Given_CartStubReturnsEmptyCart()
    {
        _cartStub
            .Given(Request.Create().WithPath("/cart").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { Items = Array.Empty<object>(), ItemCount = 0, GrandTotal = 0m }));
    }

    public void Given_CartStubReturnsCartWithItems(params (Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)[] items)
    {
        var payloadItems = items.Select(i => new
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            LineTotal = i.UnitPrice * i.Quantity
        }).ToArray();
        var grandTotal = items.Sum(i => i.UnitPrice * i.Quantity);

        _cartStub
            .Given(Request.Create().WithPath("/cart").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { Items = payloadItems, ItemCount = items.Length, GrandTotal = grandTotal }));
    }

    public void Given_CartStubAcceptsClear()
    {
        _cartStub
            .Given(Request.Create().WithPath("/cart").UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(204));
    }

    /// <summary>
    /// Clears the OrderReadModels table (cascades to LineItems) for test isolation.
    /// PostgresFixture's container/database is shared across every [Fact] in this class
    /// (mirrors Catalog's ProductsEndpointSteps convention), so count/order-sensitive
    /// assertions must start from a known-empty table rather than relying on execution order.
    /// </summary>
    public async Task Given_NoOrdersExist()
    {
        await using var db = await CreateDbContextAsync();
        db.OrderReadModels.RemoveRange(db.OrderReadModels);
        await db.SaveChangesAsync();
    }

    public async Task<Guid> Given_OrderReadModelExistsForUser(string userId, string status = "Pending")
    {
        var data = new OrderBuilder().WithUserId(userId).WithStatus(status).Build();

        await using var db = await CreateDbContextAsync();
        db.OrderReadModels.Add(new OrderReadModel
        {
            Id = data.Id,
            UserId = data.UserId,
            Status = data.Status,
            TotalAmount = data.TotalAmount,
            ItemCount = 1,
            LineItems = [new OrderLineItem { ProductId = Guid.NewGuid(), ProductName = "Seeded Product", UnitPrice = data.TotalAmount, Quantity = 1 }],
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.CreatedAt
        });
        await db.SaveChangesAsync();

        return data.Id;
    }

    public async Task Given_UserHasOrders(string userId, int count)
    {
        await using var db = await CreateDbContextAsync();

        // Test isolation — see Given_NoOrdersExist's remarks.
        db.OrderReadModels.RemoveRange(db.OrderReadModels);
        await db.SaveChangesAsync();

        var baseTime = DateTimeOffset.UtcNow;
        for (var i = 0; i < count; i++)
        {
            var data = new OrderBuilder()
                .WithUserId(userId)
                .WithCreatedAt(baseTime.AddMinutes(-i))
                .Build();

            db.OrderReadModels.Add(new OrderReadModel
            {
                Id = data.Id,
                UserId = data.UserId,
                Status = data.Status,
                TotalAmount = data.TotalAmount,
                ItemCount = 1,
                LineItems = [new OrderLineItem { ProductId = Guid.NewGuid(), ProductName = $"Product {i}", UnitPrice = data.TotalAmount, Quantity = 1 }],
                CreatedAt = data.CreatedAt,
                UpdatedAt = data.CreatedAt
            });
        }

        await db.SaveChangesAsync();
    }

    // ----- When -----

    public async Task<HttpResponseMessage> When_CheckoutIsCalled(string userId, Guid checkoutId, bool simulatePaymentFailure = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/orders/checkout")
        {
            Content = JsonContent.Create(new
            {
                messageId = Guid.NewGuid(),
                correlationId = checkoutId,
                causationId = Guid.Empty,
                occurredAt = DateTimeOffset.UtcNow,
                checkoutId,
                simulatePaymentFailure
            })
        };
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> When_GetOrderByIdIsCalled(string userId, Guid orderId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/orders/{orderId}");
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> When_GetOrdersIsCalled(string userId, int page = 1, int pageSize = 12)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/orders?page={page}&pageSize={pageSize}");
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    public async Task<(HttpResponseMessage Response, OrderDto? Body)> When_PollingUntilOrderIsVisible(
        string userId, Guid orderId, int maxAttempts = 5, int delayMs = 250)
    {
        HttpResponseMessage response = null!;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            response = await When_GetOrderByIdIsCalled(userId, orderId);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var body = await response.Content.ReadFromJsonAsync<OrderDto>();
                return (response, body);
            }

            await Task.Delay(delayMs);
        }

        return (response, null);
    }

    // ----- Then -----

    public void Then_ResponseIs200(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.OK);

    public void Then_ResponseIs202(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.Accepted);

    public void Then_ResponseIs400(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    public void Then_ResponseIs404(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    public async Task<Guid> Then_ResponseContainsOrderId(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!.Should().ContainKey("orderId");
        return Guid.Parse(body["orderId"].ToString()!);
    }

    public async Task<PagedResult<OrderSummaryDto>> Then_ResponseIs200WithPagedResult(HttpResponseMessage response, int expectedItemCount, int expectedTotalCount)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(expectedItemCount);
        body.TotalCount.Should().Be(expectedTotalCount);
        return body;
    }

    // Takes the already-parsed body rather than re-reading HttpContent — HttpContent's
    // underlying stream can only be read once; a second ReadFromJsonAsync call on the same
    // HttpResponseMessage throws ObjectDisposedException.
    public void Then_ResponseOrdersAreOrderedByCreatedAtDescending(PagedResult<OrderSummaryDto> body)
        => body.Items.Should().BeInDescendingOrder(o => o.CreatedAt);

    public void Then_ResponseOrderHasFailureReason(OrderDto? body, string expectedReason)
        => body!.FailureReason.Should().Be(expectedReason);

    public int DeleteCartCallCount()
        => _cartStub.LogEntries.Count(e => e.RequestMessage.Path == "/cart" && e.RequestMessage.Method == "DELETE");

    public int GetCartCallCount()
        => _cartStub.LogEntries.Count(e => e.RequestMessage.Path == "/cart" && e.RequestMessage.Method == "GET");

    public void Dispose()
    {
        _cartStub.Stop();
        _cartStub.Dispose();
        _client?.Dispose();
        _factory.Dispose();
    }
}
