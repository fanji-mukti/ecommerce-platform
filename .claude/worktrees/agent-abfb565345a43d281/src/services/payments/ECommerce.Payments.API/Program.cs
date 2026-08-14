using ECommerce.Payments.API.Data;
using ECommerce.Payments.API.Features.Payments;
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

    builder.AddNpgsqlDbContext<PaymentsDbContext>("postgres");

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<AuthorisePaymentConsumer>();
        x.AddConsumer<RefundPaymentConsumer>();

        x.AddEntityFrameworkOutbox<PaymentsDbContext>(o =>
        {
            o.UsePostgres();
            o.UseBusOutbox(); // Payments both consumes and publishes, so the bus outbox drainer is required
        });

        x.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            cfg.UseEntityFrameworkOutbox<PaymentsDbContext>(context);
        });

        var messagingConnectionString = builder.Configuration.GetConnectionString("messaging");
        if (messagingConnectionString == "placeholder")
        {
            // Test sentinel (see the established "placeholder" convention across services) — no
            // live Azure Service Bus is available in integration tests. Use MassTransit's
            // in-memory transport so the bus outbox drainer can still deliver
            // PaymentAuthorised/PaymentFailed/PaymentRefunded within the same test process.
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

    builder.Services.AddHostedService<DbInitializer>();

    // Payments has NO public HTTP endpoints and NO JWT auth wiring needed — it communicates
    // exclusively over the bus.
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
