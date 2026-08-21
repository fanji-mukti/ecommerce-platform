using ECommerce.Fulfillment.API.Data;
using ECommerce.Fulfillment.API.Features.Fulfillment;
using MassTransit;
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

    builder.AddNpgsqlDbContext<FulfillmentDbContext>("postgres");

    builder.Services.Configure<FulfillmentOptions>(
        builder.Configuration.GetSection(FulfillmentOptions.SectionName));

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<OrderPaidConsumer>();

        x.AddEntityFrameworkOutbox<FulfillmentDbContext>(o =>
        {
            o.UsePostgres();
            o.UseBusOutbox(); // Fulfillment publishes OrderShipped, so the bus outbox drainer is required
        });

        x.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            cfg.UseEntityFrameworkOutbox<FulfillmentDbContext>(context);
        });

        var messagingConnectionString = builder.Configuration.GetConnectionString("messaging");
        if (messagingConnectionString == "placeholder")
        {
            // Test sentinel (see Orders.API/CatalogWebApplicationFactory's established
            // "placeholder" convention) — no live Azure Service Bus is available in integration
            // tests. Use MassTransit's in-memory transport with the Quartz-backed in-memory
            // scheduler so SchedulePublish<OrderShipped> can still be exercised in-process.
            x.UsingInMemory((context, cfg) =>
            {
                cfg.UseInMemoryScheduler();
                cfg.ConfigureEndpoints(context);
            });
        }
        else
        {
            x.AddServiceBusMessageScheduler();
            x.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(messagingConnectionString);
                cfg.UseServiceBusMessageScheduler();
                cfg.ConfigureEndpoints(context);
            });
        }
    });

    builder.Services.AddHostedService<DbInitializer>();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.MapOpenApi();
    app.MapHealthChecks("/health");

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
