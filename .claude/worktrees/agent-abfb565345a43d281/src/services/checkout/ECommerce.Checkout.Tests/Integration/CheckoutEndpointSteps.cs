using System.Net;
using System.Net.Http.Json;
using ECommerce.Checkout.API.Features.Checkout;
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
using Xunit;

namespace ECommerce.Checkout.Tests.Integration;

/// <summary>
/// WebApplicationFactory for Checkout integration tests. Forces MassTransit onto the in-memory
/// transport (Program.cs's "placeholder" sentinel), points the typed Orders HTTP client at a
/// WireMock stub, and replaces the real JwtBearer scheme with the shared TestAuthHandler.
/// Checkout.API has no database — unlike OrdersWebApplicationFactory, there is no DbInitializer
/// to remove and no postgres connection string to wire.
/// </summary>
internal sealed class CheckoutWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _ordersBaseAddress;

    public CheckoutWebApplicationFactory(string ordersBaseAddress)
    {
        _ordersBaseAddress = ordersBaseAddress;

        // Program.cs's AddMassTransit(...) call reads ConnectionStrings:messaging eagerly, before
        // WebApplicationFactory's ConfigureAppConfiguration override can reach it (same eagerness
        // rationale as OrdersWebApplicationFactory) — set the env var before the host is built.
        Environment.SetEnvironmentVariable("ConnectionStrings__messaging", "placeholder");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var ordersBaseAddress = _ordersBaseAddress;

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = "placeholder"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Point the typed Orders HTTP client at the WireMock stub instead of http://orders.
            services.AddHttpClient<IOrdersClient, OrdersClient>(c => c.BaseAddress = new Uri(ordersBaseAddress));
        });
    }
}

public class CheckoutEndpointSteps : IAsyncLifetime
{
    private WireMockServer? _ordersStub;
    private CheckoutWebApplicationFactory? _factory;
    private HttpClient? _client;

    public ValueTask InitializeAsync()
    {
        _ordersStub = WireMockServer.Start();
        _factory = new CheckoutWebApplicationFactory(_ordersStub.Urls[0]);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        _ordersStub?.Stop();
        _ordersStub?.Dispose();
        return ValueTask.CompletedTask;
    }

    private WireMockServer OrdersStub => _ordersStub ?? throw new InvalidOperationException("InitializeAsync must run first.");

    private HttpClient Client => _client ??= _factory!.CreateClient();

    // ----- Given -----

    public void Given_OrdersStubAcceptsCheckout(Guid checkoutId)
    {
        OrdersStub
            .Given(Request.Create().WithPath("/orders/checkout").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(202)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { orderId = checkoutId }));
    }

    public void Given_OrdersStubRejectsEmptyCart()
    {
        OrdersStub
            .Given(Request.Create().WithPath("/orders/checkout").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = "Cart is empty." }));
    }

    public void Given_OrdersStubReturns404ForStatus(Guid checkoutId)
    {
        OrdersStub
            .Given(Request.Create().WithPath($"/orders/{checkoutId}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = "Order not found." }));
    }

    public void Given_OrdersStubReturnsStatus(Guid checkoutId, string status, string? failureReason)
    {
        OrdersStub
            .Given(Request.Create().WithPath($"/orders/{checkoutId}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = checkoutId, status, failureReason }));
    }

    // ----- When -----

    public async Task<HttpResponseMessage> When_CheckoutIsCalled(string userId, bool simulatePaymentFailure = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/checkout")
        {
            Content = JsonContent.Create(new { simulatePaymentFailure })
        };
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> When_GetCheckoutStatusIsCalled(string userId, Guid checkoutId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/checkout/{checkoutId}");
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> When_SimulateFulfillmentFailureIsCalled(string userId, Guid checkoutId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/checkout/{checkoutId}/simulate-fulfillment-failure");
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    // ----- Then -----

    public void Then_ResponseIs202(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.Accepted);

    public void Then_ResponseIs400(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    public void Then_ResponseIs404(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    public async Task<Guid> Then_ResponseContainsCheckoutId(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!.Should().ContainKey("checkoutId");
        return Guid.Parse(body["checkoutId"].ToString()!);
    }

    public async Task<CheckoutStatusDto?> Then_ResponseIs200(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<CheckoutStatusDto>();
    }

    public void Then_StatusIs(CheckoutStatusDto? body, string expected)
    {
        body.Should().NotBeNull();
        body!.Status.Should().Be(expected);
    }
}
