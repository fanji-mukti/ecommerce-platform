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

        x.AddEntityFrameworkOutbox<OrdersDbContext>(o =>
        {
            o.UsePostgres();
            o.UseBusOutbox(); // Orders publishes domain events, so the bus outbox drainer is required
        });

        x.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            cfg.UseEntityFrameworkOutbox<OrdersDbContext>(context);
        });

        x.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(builder.Configuration.GetConnectionString("messaging"));
            cfg.ConfigureEndpoints(context);
        });
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

    // OrdersEndpoints.Map(app), OrderReadModelProjector, and ICartClient are added in Plan 03-03.

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
