using ECommerce.Orders.API.Data;
using ECommerce.Orders.API.Features.Orders;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Trace;

// Bootstrap logger — captures startup failures before host is built
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddOpenApi();
    builder.Services.AddHealthChecks();

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter());

    builder.AddNpgsqlDbContext<OrdersDbContext>("postgres");

    builder.Services.AddMassTransit(x =>
    {
        x.AddSagaStateMachine<OrderStateMachine, Order>()
            .EntityFrameworkRepository(r =>
            {
                r.ExistingDbContext<OrdersDbContext>();
                r.UsePostgres();
            });

        x.AddConsumer<OrderReadModelProjector>();

        x.AddEntityFrameworkOutbox<OrdersDbContext>(o =>
        {
            o.UsePostgres();
            o.UseBusOutbox(); // Orders publishes domain events, so the bus outbox drainer is required
        });

        x.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            cfg.UseEntityFrameworkOutbox<OrdersDbContext>(context);
        });

        var messagingConnectionString = builder.Configuration.GetConnectionString("messaging");
        if (messagingConnectionString == "placeholder")
        {
            // Test sentinel (see OrdersWebApplicationFactory / CatalogWebApplicationFactory's
            // established "placeholder" convention) — no live Azure Service Bus is available in
            // integration tests. Use MassTransit's in-memory transport so the bus outbox drainer
            // can still deliver OrderCreated/OrderStatusChanged to OrderReadModelProjector and the
            // saga within the same test process (ORD-04 eventual-consistency proof).
            x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        }
        else
        {
            x.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(messagingConnectionString);
                cfg.ConfigureEndpoints(context);
            });
        }
    });

    builder.Services.AddHttpClient<ICartClient, CartClient>(c => c.BaseAddress = new Uri("http://cart"));

    // Register the Mapperly-generated mapper as a DI service — without this, ASP.NET Core's
    // Minimal API parameter-source inference cannot recognize `OrderMapper mapper` as a service
    // parameter and instead infers it as a request body, which crashes route registration for
    // GET /orders and GET /orders/{id} at host startup ("Body was inferred but the method does
    // not allow inferred body parameters"). OrderMapper is a stateless generated partial class,
    // so a singleton lifetime is safe.
    builder.Services.AddSingleton<OrderMapper>();

    builder.Services.AddHostedService<DbInitializer>();

    // JWT bearer auth — validates tokens issued by Identity's OpenIddict server.
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = "http://identity";
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters.ValidateAudience = false;
        });
    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapOpenApi();
    app.MapHealthChecks("/health");

    OrdersEndpoints.Map(app);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
