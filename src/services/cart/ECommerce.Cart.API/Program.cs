using ECommerce.Cart.API.Data;
using ECommerce.Cart.API.Features.Cart;
using FluentValidation;
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

    // Redis-backed cart store (Aspire.StackExchange.Redis integration)
    builder.AddRedisClient("redis");
    builder.Services.AddScoped<ICartStore, RedisCartStore>();

    // Synchronous internal HTTP call to Catalog for price/name snapshots (D-04)
    builder.Services.AddHttpClient<ICatalogPriceClient, CatalogPriceClient>(
        c => c.BaseAddress = new Uri("http://catalog"));

    // FluentValidation
    builder.Services.AddScoped<IValidator<AddCartItemRequest>, AddCartItemRequestValidator>();
    builder.Services.AddScoped<IValidator<UpdateCartItemQuantityRequest>, UpdateCartItemQuantityRequestValidator>();

    // Mapping
    builder.Services.AddScoped<CartMapper>();

    // JWT bearer auth — validates tokens issued by Identity's OpenIddict server (T-03-01)
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.Authority = "http://identity";
            o.RequireHttpsMetadata = false;
            o.TokenValidationParameters = new()
            {
                ValidateAudience = false
            };
        });
    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.MapOpenApi();
    app.MapHealthChecks("/health");

    app.UseAuthentication();
    app.UseAuthorization();

    CartEndpoints.Map(app);

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
