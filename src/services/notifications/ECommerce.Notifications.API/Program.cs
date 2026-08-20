using ECommerce.Notifications.API.Consumers;
using ECommerce.Notifications.API.Data;
using ECommerce.Notifications.API.Features.Notifications;
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

    builder.AddNpgsqlDbContext<NotificationsDbContext>("postgres");

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<CatalogSeededConsumer>();
        x.AddConsumer<OrderPaidNotificationConsumer>();
        x.AddConsumer<OrderShippedNotificationConsumer>();
        x.AddConsumer<PaymentFailedNotificationConsumer>();
        x.AddConsumer<OrderStatusSnapshotConsumer>();

        x.AddEntityFrameworkOutbox<NotificationsDbContext>(o =>
        {
            o.UsePostgres();
            // No UseBusOutbox() — Notifications only consumes, does not publish outbox messages
        });

        x.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            cfg.UseEntityFrameworkOutbox<NotificationsDbContext>(context);
        });

        var messagingConnectionString = builder.Configuration.GetConnectionString("messaging");
        if (messagingConnectionString == "placeholder")
        {
            // Test sentinel (see Orders/Payments' established "placeholder" convention) — no live
            // Azure Service Bus is available in integration tests. Use MassTransit's in-memory
            // transport so a WebApplicationFactory-based host can start. Notifications never
            // schedules delayed messages, so no scheduler is registered in either branch.
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
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

    NotificationsEndpoints.Map(app);

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
