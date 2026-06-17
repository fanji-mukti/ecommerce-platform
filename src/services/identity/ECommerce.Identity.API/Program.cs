using ECommerce.Identity.API.Data;
using ECommerce.Identity.API.Features.Authorization;
using ECommerce.Identity.API.Features.Profile;
using ECommerce.Identity.API.Features.Registration;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Trace;
using static OpenIddict.Abstractions.OpenIddictConstants;

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

    // EF Core + Identity + OpenIddict
    builder.Services.AddDbContext<IdentityDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("postgres"));
        options.UseOpenIddict();
    });

    builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Lockout.MaxFailedAccessAttempts = 5; // ASVS T-02-02-02: brute force protection
    })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

    builder.Services.AddOpenIddict()
        .AddCore(options =>
        {
            options.UseEntityFrameworkCore()
                   .UseDbContext<IdentityDbContext>();
        })
        .AddServer(options =>
        {
            options.SetAuthorizationEndpointUris("connect/authorize")
                   .SetTokenEndpointUris("connect/token")
                   .SetUserInfoEndpointUris("connect/userinfo")
                   .SetEndSessionEndpointUris("connect/logout");

            options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Profile);
            options.AllowAuthorizationCodeFlow();
            options.RequireProofKeyForCodeExchange(); // ASVS: PKCE required for public client (T-02-02-03)

            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();

            options.UseAspNetCore()
                   .EnableAuthorizationEndpointPassthrough()
                   .EnableTokenEndpointPassthrough()
                   .EnableUserInfoEndpointPassthrough()
                   .EnableEndSessionEndpointPassthrough();
        })
        .AddValidation(options =>
        {
            options.UseLocalServer();
            options.UseAspNetCore();
        });

    builder.Services.AddCors(options => options.AddDefaultPolicy(
        policy => policy
            .WithOrigins("http://localhost:4200") // T-02-02-07: explicit origin, never AllowAnyOrigin
            .AllowAnyHeader()
            .AllowAnyMethod()));

    builder.Services.AddRazorPages();

    // Register FluentValidation validators
    builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterValidator>();

    builder.Services.AddHostedService<DbInitializer>();

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter());

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.UseCors();             // before UseAuthentication — T-02-02-07
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapOpenApi();
    app.MapHealthChecks("/health");
    app.MapRazorPages();

    // OpenIddict PKCE authorization endpoint (passthrough)
    // Cast to Delegate so ASP.NET Core route handler correctly captures the IResult return value
    app.MapGet("connect/authorize", (Delegate)AuthorizationEndpoint.Authorize);

    // Feature endpoints
    app.MapPost("/register", RegistrationEndpoints.Register);
    app.MapGet("/me", ProfileEndpoints.GetMe).RequireAuthorization();

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
