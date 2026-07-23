using System.Net;
using System.Net.Http.Json;
using ECommerce.Cart.API.Features.Cart;
using ECommerce.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ECommerce.Cart.Tests.Integration;

/// <summary>
/// WebApplicationFactory for Cart integration tests.
/// Swaps the Redis connection string, points the typed Catalog HTTP client at a WireMock
/// stub, and (by default) replaces the real JwtBearer scheme with the shared TestAuthHandler.
/// </summary>
internal sealed class CartWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _redisConnectionString;
    private readonly string _catalogBaseAddress;
    private readonly bool _useTestAuth;

    public CartWebApplicationFactory(string redisConnectionString, string catalogBaseAddress, bool useTestAuth = true)
    {
        _redisConnectionString = redisConnectionString;
        _catalogBaseAddress = catalogBaseAddress;
        _useTestAuth = useTestAuth;

        // Aspire.StackExchange.Redis's AddRedisClient reads ConnectionStrings:redis
        // EAGERLY (synchronously, before builder.Build()) in Program.cs — too early for
        // WebApplicationFactory's ConfigureAppConfiguration override (which only applies
        // once the deferred host build runs) to reach it. Setting the environment variable
        // here, before the host is constructed (CreateClient() is called lazily later),
        // ensures WebApplication.CreateBuilder's built-in AddEnvironmentVariables() source
        // has the test connection string in place before Program.cs's own code runs.
        Environment.SetEnvironmentVariable("ConnectionStrings__redis", redisConnectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var redisConnectionString = _redisConnectionString;
        var catalogBaseAddress = _catalogBaseAddress;
        var useTestAuth = _useTestAuth;

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:redis"] = redisConnectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            if (useTestAuth)
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            }

            // Point the typed Catalog HTTP client at the WireMock stub instead of http://catalog.
            services.AddHttpClient<ICatalogPriceClient, CatalogPriceClient>(c => c.BaseAddress = new Uri(catalogBaseAddress));
        });
    }
}

public class CartEndpointSteps : IDisposable
{
    private readonly WireMockServer _catalogStub = WireMockServer.Start();
    private readonly CartWebApplicationFactory _factory;
    private readonly CartWebApplicationFactory _noAuthFactory;
    private HttpClient? _client;
    private HttpClient? _noAuthClient;

    public CartEndpointSteps(RedisFixture fixture)
    {
        _factory = new CartWebApplicationFactory(fixture.ConnectionString, _catalogStub.Urls[0]);
        _noAuthFactory = new CartWebApplicationFactory(fixture.ConnectionString, _catalogStub.Urls[0], useTestAuth: false);
    }

    private HttpClient Client => _client ??= _factory.CreateClient();
    private HttpClient NoAuthClient => _noAuthClient ??= _noAuthFactory.CreateClient();

    public void Given_CatalogHasProduct(Guid productId, string name, decimal price)
    {
        _catalogStub
            .Given(Request.Create().WithPath($"/products/{productId}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    Id = productId,
                    Name = name,
                    Sku = "TST-001",
                    Price = price,
                    StockQuantity = 10,
                    Category = "Test",
                    ImageUrl = (string?)null
                }));
    }

    public void Given_CatalogProductNotFound(Guid productId)
    {
        _catalogStub
            .Given(Request.Create().WithPath($"/products/{productId}").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));
    }

    public async Task<HttpResponseMessage> When_AddItemIsCalled(string userId, Guid productId, int quantity)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/cart/items")
        {
            Content = JsonContent.Create(new AddCartItemRequest(productId, quantity))
        };
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> When_GetCartIsCalled(string userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/cart");
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> When_UpdateQuantityIsCalled(string userId, Guid productId, int quantity)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/cart/items/{productId}")
        {
            Content = JsonContent.Create(new UpdateCartItemQuantityRequest(quantity))
        };
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> When_DeleteItemIsCalled(string userId, Guid productId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/cart/items/{productId}");
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> When_ClearCartIsCalled(string userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/cart");
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> When_GetCartIsCalledWithoutAuth()
        => await NoAuthClient.GetAsync("/cart");

    public int CatalogCallCountFor(Guid productId)
        => _catalogStub.LogEntries.Count(e => e.RequestMessage.Path == $"/products/{productId}");

    public void Then_ResponseIs200(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.OK);

    public void Then_ResponseIs204(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.NoContent);

    public void Then_ResponseIs400(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    public void Then_ResponseIs404(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    public void Then_ResponseIs401(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    public async Task<CartDto> Then_ResponseContainsCart(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CartDto>();
        body.Should().NotBeNull();
        return body!;
    }

    public void Dispose()
    {
        _catalogStub.Stop();
        _catalogStub.Dispose();
        _client?.Dispose();
        _noAuthClient?.Dispose();
        _factory.Dispose();
        _noAuthFactory.Dispose();
    }
}
