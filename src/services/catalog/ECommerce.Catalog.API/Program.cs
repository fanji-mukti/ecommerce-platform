using ECommerce.Catalog.API.Data;
using ECommerce.Catalog.API.Features.Products;
using MassTransit;
using Microsoft.EntityFrameworkCore;
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

    builder.Services.AddDbContext<CatalogDbContext>(options =>
    {
        var constr = builder.Configuration.GetConnectionString("postgres");
        options.UseNpgsql(builder.Configuration.GetConnectionString("postgres"));
    });


    // To  how to use MassTransit with Azure Service Bus and Entity Framework Outbox in this context.
    //builder.Services.AddMassTransit(x =>
    //{
    //    x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
    //    {
    //        o.UsePostgres();
    //        o.UseBusOutbox(); // enables outbox drainer background service
    //    });

    //    x.UsingAzureServiceBus((context, cfg) =>
    //    {
    //        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
    //        cfg.ConfigureEndpoints(context);
    //    });
    //});

    // Register DbInitializer as a hosted service to run on startup
    // This for demo purposes only. In production, we would typically already have the database.
    builder.Services.AddHostedService<DbInitializer>();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.MapOpenApi();
    app.MapHealthChecks("/health");
    ProductsEndpoints.Map(app);

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
