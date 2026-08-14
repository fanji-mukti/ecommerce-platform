using ECommerce.Checkout.API.Features.Checkout;
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

    // Producer-only MassTransit registration — Checkout.API has no saga, no consumer, no
    // outbox/DbContext. It only ever calls IPublishEndpoint.Publish (the demo-only
    // simulate-fulfillment-failure trigger). Same "placeholder" test-transport sentinel used by
    // Orders.API/Payments.API.
    builder.Services.AddMassTransit(x =>
    {
        var messagingConnectionString = builder.Configuration.GetConnectionString("messaging");
        if (messagingConnectionString == "placeholder")
        {
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

    builder.Services.AddHttpClient<IOrdersClient, OrdersClient>(c => c.BaseAddress = new Uri("http://orders"));

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

    CheckoutEndpoints.Map(app);

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
