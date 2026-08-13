using ECommerce.Notifications.API.Consumers;
using ECommerce.Notifications.API.Data;
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

    builder.AddNpgsqlDbContext<NotificationsDbContext>("postgres");

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<CatalogSeededConsumer>();

        x.AddEntityFrameworkOutbox<NotificationsDbContext>(o =>
        {
            o.UsePostgres();
            // No UseBusOutbox() — Notifications only consumes, does not publish outbox messages
        });

        x.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            cfg.UseEntityFrameworkOutbox<NotificationsDbContext>(context);
        });

        x.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(builder.Configuration.GetConnectionString("messaging"));
            cfg.ConfigureEndpoints(context);
        });
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
