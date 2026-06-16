# Phase 2: Identity, Catalog & Gateway - Pattern Map

**Mapped:** 2026-06-17
**Files analyzed:** 32 (new/modified files across 6 pillars)
**Analogs found:** 9 / 32 (codebase is Phase 1 scaffolds only — most analogs are within-project; research patterns fill the rest)

---

## Codebase State Note

Phase 1 produced uniform service stubs. Every `Program.cs` is identical (Serilog bootstrap + OTel + `/health`). Every `.csproj` is identical (net10.0 + ImplicitUsings + Nullable + same 6 packages + Contracts reference). Every Contracts placeholder follows the same `record : IMessageEnvelope` shape. The `.sln` files all reference the API project + Contracts via relative path. There are no Angular files, no test projects, and no EF Core code in the codebase yet.

**Implication for the planner:** The primary analog for all new `Program.cs` expansions is the existing stub. Every new service file must preserve the Serilog bootstrap + OTel block and add on top — never replace it. For patterns with no codebase analog (EF Core DbContext, MassTransit, OpenIddict, Angular), the RESEARCH.md code examples are the authoritative source; those are cited below.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/building-blocks/Contracts/Catalog/Events/V1/CatalogSeeded.cs` | model/contract | event-driven | `src/building-blocks/Contracts/Cart/Events/V1/Placeholder.cs` | exact |
| `src/building-blocks/Contracts/Contracts.csproj` | config | — | `src/building-blocks/Contracts/Contracts.csproj` (self, no change) | exact |
| `src/building-blocks/Tests.Common/ECommerce.Tests.Common.csproj` | config | — | `src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj` | role-match |
| `src/building-blocks/Tests.Common/PostgresFixture.cs` | utility | batch | no analog | none |
| `src/building-blocks/Tests.Common/ServiceWebApplicationFactory.cs` | utility | request-response | no analog | none |
| `src/building-blocks/Tests.Common/Builders/ProductBuilder.cs` | utility | transform | no analog | none |
| `src/building-blocks/Tests.Common/Builders/UserBuilder.cs` | utility | transform | no analog | none |
| `src/services/identity/ECommerce.Identity.API/Program.cs` | config/middleware | request-response | `src/services/identity/ECommerce.Identity.API/Program.cs` (existing stub) | exact |
| `src/services/identity/ECommerce.Identity.API/ECommerce.Identity.API.csproj` | config | — | `src/services/identity/ECommerce.Identity.API/ECommerce.Identity.API.csproj` (existing) | exact |
| `src/services/identity/ECommerce.Identity.API/Data/IdentityDbContext.cs` | model | CRUD | no analog | none |
| `src/services/identity/ECommerce.Identity.API/Data/DbInitializer.cs` | service | batch | no analog | none |
| `src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterEndpoint.cs` | controller | request-response | no analog | none |
| `src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterRequest.cs` | model | — | no analog | none |
| `src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterValidator.cs` | middleware | — | no analog | none |
| `src/services/identity/ECommerce.Identity.API/Features/Profile/MeEndpoint.cs` | controller | request-response | no analog | none |
| `src/services/identity/ECommerce.Identity.API/Features/Profile/UserProfileDto.cs` | model | — | no analog | none |
| `src/services/identity/ECommerce.Identity.API/Features/Authorization/AuthorizationEndpoint.cs` | controller | request-response | no analog | none |
| `src/services/identity/ECommerce.Identity.API/Pages/Account/Login.cshtml` | component | request-response | no analog | none |
| `src/services/identity/ECommerce.Identity.API/Pages/Account/Login.cshtml.cs` | controller | request-response | no analog | none |
| `src/services/catalog/ECommerce.Catalog.API/Program.cs` | config/middleware | request-response | `src/services/catalog/ECommerce.Catalog.API/Program.cs` (existing stub) | exact |
| `src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj` | config | — | `src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj` (existing) | exact |
| `src/services/catalog/ECommerce.Catalog.API/Data/CatalogDbContext.cs` | model | CRUD | no analog | none |
| `src/services/catalog/ECommerce.Catalog.API/Data/DbInitializer.cs` | service | batch | no analog | none |
| `src/services/catalog/ECommerce.Catalog.API/Features/Products/Product.cs` | model | — | no analog | none |
| `src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductDto.cs` | model | — | no analog | none |
| `src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductsEndpoints.cs` | controller | CRUD | no analog | none |
| `src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductMapper.cs` | utility | transform | no analog | none |
| `src/services/notifications/ECommerce.Notifications.API/Program.cs` | config/middleware | event-driven | `src/services/notifications/ECommerce.Notifications.API/Program.cs` (existing stub) | exact |
| `src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj` | config | — | `src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj` (existing) | exact |
| `src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs` | service | event-driven | no analog | none |
| `src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs` | model | CRUD | no analog | none |
| `src/services/gateway/Gateway.sln` | config | — | `src/services/catalog/Catalog.sln` | exact |
| `src/services/gateway/ECommerce.Gateway.API/Program.cs` | middleware | request-response | `src/services/catalog/ECommerce.Catalog.API/Program.cs` (existing stub) | role-match |
| `src/services/gateway/ECommerce.Gateway.API/ECommerce.Gateway.API.csproj` | config | — | `src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj` (existing) | role-match |
| `src/services/gateway/ECommerce.Gateway.API/appsettings.json` | config | request-response | no analog | none |
| `src/ecommerce.AppHost/Program.cs` | config | — | `src/ecommerce.AppHost/Program.cs` (existing — extend) | exact |
| `src/ecommerce.AppHost/ecommerce.AppHost.csproj` | config | — | `src/ecommerce.AppHost/ecommerce.AppHost.csproj` (existing — extend) | exact |
| `.github/workflows/ci.yml` | config | — | `.github/workflows/ci.yml` (existing — extend) | exact |
| `src/services/identity/ECommerce.Identity.Tests/` (4 files) | test | request-response | no analog | none |
| `src/services/catalog/ECommerce.Catalog.Tests/` (4 files) | test | CRUD | no analog | none |
| `src/services/notifications/ECommerce.Notifications.Tests/` (2 files) | test | event-driven | no analog | none |
| `src/services/gateway/ECommerce.Gateway.Tests/` (2 files) | test | request-response | no analog | none |
| Angular: `src/frontend/ecommerce-app/` (scaffold + feature components) | component | request-response | no analog | none |

---

## Pattern Assignments

### `src/building-blocks/Contracts/Catalog/Events/V1/CatalogSeeded.cs` (model/contract, event-driven)

**Analog:** `src/building-blocks/Contracts/Cart/Events/V1/Placeholder.cs`

**Full file pattern** (lines 1-11 — entire file):
```csharp
using ECommerce.Contracts;

namespace ECommerce.Cart.Events.V1;

public record CartServiceReady(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
```

**New file target** — replace Placeholder.cs at `Catalog/Events/V1/` following exact same shape, extending the primary constructor with domain-specific fields after the envelope fields:
```csharp
using ECommerce.Contracts;

namespace ECommerce.Catalog.Events.V1;

public record CatalogSeeded(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid SeedId,
    int ItemCount,
    DateTimeOffset SeededAt
) : IMessageEnvelope;
```

**Rules:**
- Namespace must stay `ECommerce.Catalog.Events.V1` (D-33)
- `using ECommerce.Contracts;` is the only using statement — Contracts library has zero dependencies
- Primary constructor: envelope fields first (matching `IMessageEnvelope` property names exactly), domain fields after
- File replaces `Placeholder.cs`; do NOT rename — delete the old file and create the new one

---

### `src/services/{service}/ECommerce.{Service}.API/Program.cs` — Shared expansion pattern (all services)

**Analog:** `src/services/identity/ECommerce.Identity.API/Program.cs` (lines 1-45)

**Existing stub to PRESERVE** (lines 1-45 — the entire stub must survive in expanded files):
```csharp
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
```

**Expansion rule:** Insert new `builder.Services.*` calls immediately after `builder.Services.AddHealthChecks()`. Insert new `app.Use*()` middleware calls after `app.UseHttpsRedirection()` and before `app.MapOpenApi()`. Insert `app.Map*()` endpoint registrations after `app.MapHealthChecks("/health")`. Never remove any of the existing lines.

---

### `src/services/identity/ECommerce.Identity.API/Program.cs` (config/middleware, request-response)

**Analog:** Existing stub + RESEARCH.md Pattern 1, Pattern 3

**Additions to make after the baseline stub:**

**Service registrations** (insert after `builder.Services.AddHealthChecks()`):
```csharp
// EF Core + Identity + OpenIddict
builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("postgres"));
    options.UseOpenIddict();
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
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
               .SetUserinfoEndpointUris("connect/userinfo")
               .SetLogoutEndpointUris("connect/logout");

        options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Profile);
        options.AllowAuthorizationCodeFlow();
        options.RequireProofKeyForCodeExchange();  // ASVS: PKCE required for public client

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserinfoEndpointPassthrough()
               .EnableLogoutEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddCors(options => options.AddDefaultPolicy(
    policy => policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddRazorPages();

builder.Services.AddHostedService<DbInitializer>();
```

**Middleware order** (insert between `app.UseHttpsRedirection()` and `app.MapOpenApi()`):
```csharp
app.UseCors();              // before UseAuthentication — see RESEARCH.md Pitfall 3
app.UseAuthentication();
app.UseAuthorization();
```

**Endpoint registrations** (insert after `app.MapHealthChecks("/health")`):
```csharp
app.MapRazorPages();
app.MapGroup("/connect").MapOpenIdConnectEndpoints();  // OpenIddict passthrough endpoints

// Feature endpoints
app.MapPost("/register", RegistrationEndpoints.Register);
app.MapGet("/me", ProfileEndpoints.GetMe).RequireAuthorization();
```

---

### `src/services/identity/ECommerce.Identity.API/ECommerce.Identity.API.csproj` (config)

**Analog:** `src/services/identity/ECommerce.Identity.API/ECommerce.Identity.API.csproj` (lines 1-22 — existing file)

**Existing baseline** (all lines):
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.8" />
    <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
    <PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.15.3" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.2" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.15.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../building-blocks/Contracts/Contracts.csproj" />
  </ItemGroup>

</Project>
```

**New packages to add** (second `<ItemGroup>` block, pinned versions per RESEARCH.md):
```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.9" />
    <PackageReference Include="OpenIddict.AspNetCore" Version="7.5.0" />
    <PackageReference Include="OpenIddict.EntityFrameworkCore" Version="7.5.0" />
    <PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="13.4.4" />
    <PackageReference Include="FluentValidation" Version="11.3.1" />
    <PackageReference Include="Riok.Mapperly" Version="4.3.1" />
  </ItemGroup>
```

**Rules:** Preserve all existing packages. Add new packages in a separate `<ItemGroup>` block after the existing one.

---

### `src/services/identity/ECommerce.Identity.API/Data/IdentityDbContext.cs` (model, CRUD)

**Analog:** None in codebase. Use RESEARCH.md Code Examples section ("IdentityDbContext").

**Pattern from RESEARCH.md:**
```csharp
public class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // OpenIddict tables registered automatically via UseEntityFrameworkCore()
    }
}
```

**Namespace:** `ECommerce.Identity.API.Data`
**Using directives:** `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Identity`, `Microsoft.EntityFrameworkCore`

---

### `src/services/identity/ECommerce.Identity.API/Data/DbInitializer.cs` (service, batch)

**Analog:** None in codebase. Use RESEARCH.md Pattern 3 (lines 463-517).

**Core pattern from RESEARCH.md:**
```csharp
public class DbInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        // Apply migrations
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync(ct);

        // Seed OpenIddict SPA client
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        if (await manager.FindByClientIdAsync("ecommerce-spa", ct) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "ecommerce-spa",
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "ECommerce SPA",
                Type = ClientTypes.Public,
                PostLogoutRedirectUris = { new Uri("http://localhost:4200") },
                RedirectUris = { new Uri("http://localhost:4200/callback") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    $"{Permissions.Prefixes.Scope}openid"
                },
            }, ct);
        }

        // Seed demo users
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        await SeedUserIfNotExists(userManager, "demo@example.com", "demo123", ct);
        await SeedUserIfNotExists(userManager, "admin@example.com", "admin123", ct);
    }

    private static async Task SeedUserIfNotExists(UserManager<IdentityUser> um,
        string email, string password, CancellationToken ct)
    {
        if (await um.FindByEmailAsync(email) is null)
        {
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            await um.CreateAsync(user, password);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

**Namespace:** `ECommerce.Identity.API.Data`

---

### `src/services/identity/ECommerce.Identity.API/Features/Authorization/AuthorizationEndpoint.cs` (controller, request-response)

**Analog:** None in codebase. Use RESEARCH.md Pattern 2.

**Core pattern from RESEARCH.md (lines 423-457):**
```csharp
// Map in Program.cs; this file contains the handler logic
app.MapGet("/connect/authorize", async (HttpContext ctx) =>
{
    var request = ctx.GetOpenIddictServerRequest()
        ?? throw new InvalidOperationException("OpenIddict request missing.");

    var result = await ctx.AuthenticateAsync(IdentityConstants.ApplicationScheme);
    if (!result.Succeeded)
    {
        var redirectUri = QueryHelpers.AddQueryString(
            "/Account/Login",
            new Dictionary<string, string?> { ["returnUrl"] = ctx.Request.PathBase + ctx.Request.Path + ctx.Request.QueryString });

        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = redirectUri },
            [IdentityConstants.ApplicationScheme]);
    }

    var user = result.Principal;
    var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    identity.AddClaim(Claims.Subject, user.FindFirstValue(ClaimTypes.NameIdentifier)!,
        Destinations.AccessToken, Destinations.IdentityToken);
    identity.AddClaim(Claims.Email, user.FindFirstValue(ClaimTypes.Email)!,
        Destinations.IdentityToken);
    identity.AddClaim(Claims.Name, user.FindFirstValue(ClaimTypes.Name)!,
        Destinations.IdentityToken);

    var principal = new ClaimsPrincipal(identity);
    principal.SetScopes(request.GetScopes());

    return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});
```

**Note:** RESEARCH.md open question OQ-2 flags uncertainty about whether Minimal API passthrough works without MVC controllers. The safer fallback is to use a minimal MVC controller per official OpenIddict samples. If `MapGet("/connect/authorize", ...)` does not function at runtime, add `builder.Services.AddControllers()` + `app.MapControllers()` and convert this to a `[Controller]` class.

---

### `src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterEndpoint.cs` (controller, request-response)

**Analog:** None in codebase. Closest structural analog is any Minimal API endpoint + FluentValidation.

**Pattern (from RESEARCH.md architecture + ASVS security checklist):**
```csharp
// Namespace: ECommerce.Identity.API.Features.Registration
public static class RegistrationEndpoints
{
    public static async Task<IResult> Register(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        UserManager<IdentityUser> userManager,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var user = new IdentityUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            // 409 for duplicate email; 400 for other failures
            var isDuplicate = result.Errors.Any(e => e.Code == "DuplicateUserName");
            return isDuplicate
                ? Results.Conflict(new { error = "Email already in use." })
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        return Results.Created($"/me", new { email = request.Email });
    }
}
```

**Error handling rule:** Use `Results.Problem()` (RFC 7807) for unexpected failures. Use `Results.ValidationProblem()` for FluentValidation failures. Never expose stack traces. (RESEARCH.md ASVS V7)

---

### `src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterValidator.cs` (middleware, validation)

**Analog:** None in codebase. Standard FluentValidation pattern.

**Pattern:**
```csharp
// Namespace: ECommerce.Identity.API.Features.Registration
public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);  // ASVS V2: 8+ char minimum
    }
}
```

---

### `src/services/identity/ECommerce.Identity.API/Features/Profile/MeEndpoint.cs` (controller, request-response)

**Analog:** None in codebase.

**Pattern:**
```csharp
// Namespace: ECommerce.Identity.API.Features.Profile
public static class ProfileEndpoints
{
    public static IResult GetMe(ClaimsPrincipal user)
    {
        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("email");
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Results.Ok(new UserProfileDto(sub!, email!));
    }
}
```

**Auth guard:** Registered via `.RequireAuthorization()` in Program.cs endpoint mapping — not inside the handler.

---

### `src/services/identity/ECommerce.Identity.API/Pages/Account/Login.cshtml.cs` (controller, request-response)

**Analog:** None in codebase. Standard ASP.NET Core Identity Razor Page pattern.

**Core pattern:**
```csharp
// Namespace: ECommerce.Identity.API.Pages.Account
public class LoginModel(SignInManager<IdentityUser> signInManager) : PageModel
{
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        var result = await signInManager.PasswordSignInAsync(Email, Password, false, lockoutOnFailure: true);
        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/");

        ModelState.AddModelError(string.Empty, "Invalid credentials.");
        ReturnUrl = returnUrl;
        return Page();
    }
}
```

**Lockout rule:** `lockoutOnFailure: true` implements ASVS brute-force protection (Lockout.MaxFailedAccessAttempts = 5 configured in `AddIdentity` options in Program.cs).

---

### `src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj` (config)

**Analog:** `src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj` (existing — lines 1-22)

**Same baseline as Identity.** New packages to add in a separate `<ItemGroup>`:
```xml
  <ItemGroup>
    <PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="13.4.4" />
    <PackageReference Include="MassTransit" Version="8.3.6" />
    <PackageReference Include="MassTransit.Azure.ServiceBus.Core" Version="8.3.6" />
    <PackageReference Include="MassTransit.EntityFrameworkCore" Version="8.3.6" />
    <PackageReference Include="FluentValidation" Version="11.3.1" />
    <PackageReference Include="Riok.Mapperly" Version="4.3.1" />
  </ItemGroup>
```

**CRITICAL rule:** Never use `Version="*"` or `Version="8.*"` for MassTransit — always pin `Version="8.3.6"` explicitly. See ADR-0006 and RESEARCH.md anti-patterns.

---

### `src/services/catalog/ECommerce.Catalog.API/Data/CatalogDbContext.cs` (model, CRUD)

**Analog:** None in codebase. Use RESEARCH.md Code Examples section ("EF Core DbContext with MassTransit Outbox Entities").

**Pattern from RESEARCH.md:**
```csharp
// Namespace: ECommerce.Catalog.API.Data
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // MassTransit outbox/inbox tables — required for transactional outbox
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Price).HasColumnType("decimal(18,2)");
            b.Property(p => p.Category).HasMaxLength(100);
            b.Property(p => p.Sku).HasMaxLength(50);
            b.Property(p => p.Description).HasMaxLength(2000);
        });
    }
}
```

**Rule:** `AddInboxStateEntity()`, `AddOutboxMessageEntity()`, `AddOutboxStateEntity()` must all three be present when MassTransit outbox is enabled — they create the required MT tables.

---

### `src/services/catalog/ECommerce.Catalog.API/Data/DbInitializer.cs` (service, batch)

**Analog:** None in codebase. Same `IHostedService` pattern as Identity's `DbInitializer` (RESEARCH.md Pattern 3 shape), but for catalog seeding + outbox publishing.

**Core pattern:**
```csharp
// Namespace: ECommerce.Catalog.API.Data
public class DbInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.MigrateAsync(ct);

        if (await db.Products.AnyAsync(ct))
            return;  // idempotent — only seed once

        var products = BuildSeedProducts();  // 20-50 SKUs
        db.Products.AddRange(products);

        // Publish CatalogSeeded via MassTransit transactional outbox
        // publishEndpoint is obtained from the DI scope
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var seedId = Guid.NewGuid();
        await publishEndpoint.Publish(new CatalogSeeded(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            SeedId: seedId,
            ItemCount: products.Count,
            SeededAt: DateTimeOffset.UtcNow), ct);

        await db.SaveChangesAsync(ct);  // SaveChanges commits product rows + outbox message atomically
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

**Critical rule (RESEARCH.md Pitfall 5):** Always set `MessageId`, `CorrelationId`, `OccurredAt` explicitly. MassTransit sets its own transport-level `MessageId` independently; the record property `CatalogSeeded.MessageId` is the application-level envelope ID used by the inbox deduplication.

---

### `src/services/catalog/ECommerce.Catalog.API/Features/Products/Product.cs` (model)

**Analog:** None in codebase. From RESEARCH.md Code Examples ("Product entity — Claude's Discretion fields").

**Pattern from RESEARCH.md:**
```csharp
// Namespace: ECommerce.Catalog.API.Features.Products
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

---

### `src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductsEndpoints.cs` (controller, CRUD)

**Analog:** None in codebase. Use RESEARCH.md Code Examples ("Catalog Products Endpoint — Minimal API").

**GET /products pattern from RESEARCH.md:**
```csharp
// Namespace: ECommerce.Catalog.API.Features.Products
public static class ProductsEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/products", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] string? category,
            CatalogDbContext db,
            CancellationToken ct) =>
        {
            // Input validation (ASVS V5)
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 12;

            var query = db.Products.AsQueryable();
            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            var total = await query.CountAsync(ct);
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto(p.Id, p.Name, p.Sku, p.Price, p.StockQuantity, p.Category, p.ImageUrl))
                .ToListAsync(ct);

            return Results.Ok(new { Items = products, TotalCount = total, Page = page, PageSize = pageSize });
        });

        app.MapGet("/products/{id:guid}", async (Guid id, CatalogDbContext db, CancellationToken ct) =>
        {
            var product = await db.Products.FindAsync([id], ct);
            return product is null
                ? Results.NotFound(new { error = "Product not found." })
                : Results.Ok(new ProductDto(product.Id, product.Name, product.Sku, product.Price,
                    product.StockQuantity, product.Category, product.ImageUrl));
        });
    }
}
```

**Error format rule (ASVS V7):** Use `Results.NotFound(new { error = "..." })` or `Results.Problem()` — never expose EF Core exception messages.

---

### `src/services/catalog/ECommerce.Catalog.API/Program.cs` (config/middleware, request-response)

**Analog:** Existing stub (identical to Identity stub pattern — lines 1-45 shown above)

**Service registrations to add after `builder.Services.AddHealthChecks()`:**
```csharp
builder.AddNpgsqlDbContext<CatalogDbContext>("postgres");

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();  // enables outbox drainer background service
    });

    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddHostedService<DbInitializer>();
```

**Endpoint additions:**
```csharp
ProductsEndpoints.Map(app);
```

---

### `src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj` (config)

**Analog:** `src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj` (existing — lines 1-22)

**New packages to add** (same pattern as Catalog but without Mapperly/FluentValidation):
```xml
  <ItemGroup>
    <PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="13.4.4" />
    <PackageReference Include="MassTransit" Version="8.3.6" />
    <PackageReference Include="MassTransit.Azure.ServiceBus.Core" Version="8.3.6" />
    <PackageReference Include="MassTransit.EntityFrameworkCore" Version="8.3.6" />
  </ItemGroup>
```

---

### `src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs` (service, event-driven)

**Analog:** None in codebase. Use RESEARCH.md Pattern 5 shape.

**Pattern:**
```csharp
// Namespace: ECommerce.Notifications.API.Consumers
public class CatalogSeededConsumer(NotificationsDbContext db, ILogger<CatalogSeededConsumer> logger)
    : IConsumer<CatalogSeeded>
{
    public async Task Consume(ConsumeContext<CatalogSeeded> context)
    {
        var msg = context.Message;
        logger.LogInformation("CatalogSeeded received: SeedId={SeedId}, ItemCount={ItemCount}",
            msg.SeedId, msg.ItemCount);

        // Idempotency note: MassTransit inbox deduplicates by transport MessageId.
        // This consumer body runs exactly once per unique delivery.
        // Phase 2: no real notification logic — log-only to demonstrate inbox receipt.

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
```

---

### `src/services/notifications/ECommerce.Notifications.API/Program.cs` (config/middleware, event-driven)

**Analog:** Existing stub (lines 1-45)

**Service registrations to add:**
```csharp
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

builder.Services.AddHostedService<DbInitializer>();  // applies migrations
```

---

### `src/services/gateway/Gateway.sln` (config)

**Analog:** `src/services/catalog/Catalog.sln` (lines 1-49 — full file)

**Pattern to copy exactly**, substituting Gateway names:
- Project name: `ECommerce.Gateway.API`
- Project path: `ECommerce.Gateway.API\ECommerce.Gateway.API.csproj`
- GUIDs: generate fresh GUIDs (do not copy from Catalog.sln)
- Contracts relative path: `..\..\..\building-blocks\Contracts\Contracts.csproj` (same depth)

---

### `src/services/gateway/ECommerce.Gateway.API/ECommerce.Gateway.API.csproj` (config)

**Analog:** `src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj` (existing — lines 1-22)

**Baseline PropertyGroup:** identical (net10.0, ImplicitUsings, Nullable enable)
**Existing packages:** KEEP Serilog, OTel packages (same versions)
**Remove:** `Microsoft.AspNetCore.OpenApi` (gateway needs no OpenAPI — it forwards, not serves endpoints)
**Replace base packages with:** keep Serilog + OTel, add:
```xml
  <ItemGroup>
    <PackageReference Include="Yarp.ReverseProxy" Version="2.3.0" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery.Yarp" Version="10.7.0" />
  </ItemGroup>
```

**Note:** Gateway does NOT get MassTransit, EF Core, FluentValidation, or Mapperly packages.

---

### `src/services/gateway/ECommerce.Gateway.API/Program.cs` (middleware, request-response)

**Analog:** Existing stub for Serilog/OTel bootstrap; RESEARCH.md Pattern 7 for YARP wiring.

**Complete Program.cs** (Serilog bootstrap preserved + YARP-specific service registration):
```csharp
using Serilog;
using Serilog.Events;
using OpenTelemetry.Trace;

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

    builder.Services.AddHealthChecks();

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter());

    // YARP reverse proxy with Aspire service discovery
    builder.Services
        .AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
        .AddServiceDiscoveryDestinationResolver();

    var app = builder.Build();

    app.MapHealthChecks("/health");
    app.MapReverseProxy();

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
```

**Rules:**
- No `app.UseHttpsRedirection()` on the gateway — it receives from Aspire on HTTP internally
- No `app.MapOpenApi()` — gateway serves no API schema
- No JWT validation — gateway only forwards `Authorization` header (D-08)

---

### `src/services/gateway/ECommerce.Gateway.API/appsettings.json` (config, request-response)

**Analog:** None in codebase. Use RESEARCH.md Pattern 7.

**Full appsettings.json pattern:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      "identity-route": {
        "ClusterId": "identity",
        "Match": { "Path": "/api/identity/{**catch-all}" },
        "Transforms": [{ "PathRemovePrefix": "/api/identity" }]
      },
      "catalog-route": {
        "ClusterId": "catalog",
        "Match": { "Path": "/api/catalog/{**catch-all}" },
        "Transforms": [{ "PathRemovePrefix": "/api/catalog" }]
      },
      "notifications-route": {
        "ClusterId": "notifications",
        "Match": { "Path": "/api/notifications/{**catch-all}" },
        "Transforms": [{ "PathRemovePrefix": "/api/notifications" }]
      }
    },
    "Clusters": {
      "identity": {
        "Destinations": {
          "identity": { "Address": "http://identity" }
        }
      },
      "catalog": {
        "Destinations": {
          "catalog": { "Address": "http://catalog" }
        }
      },
      "notifications": {
        "Destinations": {
          "notifications": { "Address": "http://notifications" }
        }
      }
    }
  }
}
```

**Critical rule (RESEARCH.md Pitfall 6):** The cluster destination names (`"identity"`, `"catalog"`, `"notifications"`) must exactly match the Aspire AppHost `AddProject<T>("name")` registration strings. The AppHost currently uses `"catalog"`, `"identity"`, `"notifications"` — these match.

---

### `src/ecommerce.AppHost/Program.cs` (config — extend existing)

**Analog:** `src/ecommerce.AppHost/Program.cs` (lines 1-99 — existing file shown above)

**Addition to make** — insert after the `notifications` block and before `AddDockerComposeEnvironment`:
```csharp
var gateway = builder.AddProject<Projects.ECommerce_Gateway_API>("gateway")
    .WithEndpoint(
        name: "http",
        port: 5000,
        targetPort: 5000,
        scheme: "http",
        isExternal: true)
    .WithExternalHttpEndpoints()
    .WithReference(catalog)    // service discovery resolves "catalog" for YARP
    .WithReference(identity)   // service discovery resolves "identity" for YARP
    .WithReference(notifications);  // service discovery resolves "notifications" for YARP
```

**Rule:** The `catalog`, `identity`, and `notifications` variables must be captured from their `AddProject` calls above (currently those calls are chained without variable assignment — refactor to assign `var catalog = builder.AddProject<...>("catalog")...` etc., then `.WithReference(catalog)` on the gateway).

---

### `src/ecommerce.AppHost/ecommerce.AppHost.csproj` (config — extend existing)

**Analog:** `src/ecommerce.AppHost/ecommerce.AppHost.csproj` (lines 1-26 — existing file)

**Addition to make** — add to the existing `<ItemGroup>` of `<ProjectReference>` entries:
```xml
    <ProjectReference Include="../services/gateway/ECommerce.Gateway.API/ECommerce.Gateway.API.csproj" />
```

---

### `.github/workflows/ci.yml` (config — extend existing)

**Analog:** `.github/workflows/ci.yml` (lines 1-42 — existing file)

**Addition to make** — add to the `matrix.solution` list:
```yaml
          - src/services/gateway/Gateway.sln
          - src/services/identity/ECommerce.Identity.Tests/Identity.Tests.sln
          - src/services/catalog/ECommerce.Catalog.Tests/Catalog.Tests.sln
          - src/services/notifications/ECommerce.Notifications.Tests/Notifications.Tests.sln
          - src/services/gateway/ECommerce.Gateway.Tests/Gateway.Tests.sln
```

**Test step addition** — add `--collect "XPlat Code Coverage"` to the dotnet test command:
```yaml
      - name: Test
        run: dotnet test ${{ matrix.solution }} --no-build --configuration Release --collect "XPlat Code Coverage"
```

---

### `src/building-blocks/Tests.Common/PostgresFixture.cs` (utility, batch)

**Analog:** None in codebase. Use RESEARCH.md Pattern 10.

**Pattern from RESEARCH.md:**
```csharp
// Namespace: ECommerce.Tests.Common
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithDatabase("test-db")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();
    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
```

**Usage pattern** (per D-28 — per-class isolation):
```csharp
// In each Integration test class:
public class ProductsEndpointTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    // each test class gets its own Postgres container
}
```

---

### `src/building-blocks/Tests.Common/Builders/ProductBuilder.cs` (utility, transform)

**Analog:** None in codebase. Builder pattern from CONTEXT.md D-27/specifics section.

**Pattern:**
```csharp
// Namespace: ECommerce.Tests.Common.Builders
public class ProductBuilder
{
    private string _name = "Test Product";
    private string _sku = "TST-001";
    private string _description = "A test product.";
    private decimal _price = 9.99m;
    private int _stock = 100;
    private string _category = "Electronics";
    private string? _imageUrl = null;

    public ProductBuilder WithName(string name) { _name = name; return this; }
    public ProductBuilder WithPrice(decimal price) { _price = price; return this; }
    public ProductBuilder WithCategory(string category) { _category = category; return this; }
    public ProductBuilder WithStock(int stock) { _stock = stock; return this; }
    public ProductBuilder WithSku(string sku) { _sku = sku; return this; }

    public Product Build() => new()
    {
        Id = Guid.NewGuid(),
        Name = _name,
        Sku = _sku,
        Description = _description,
        Price = _price,
        StockQuantity = _stock,
        Category = _category,
        ImageUrl = _imageUrl,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
```

---

### Test project `*Tests.cs` / `*Steps.cs` files (test, CRUD/request-response/event-driven)

**Analog:** None in codebase. Use D-25 two-class pattern.

**Test class pattern** (D-25):
```csharp
// Namespace: ECommerce.Catalog.Tests.Integration.Products
// File: GetProductsTests.cs
public class GetProductsTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly GetProductsSteps _steps = new(fixture);

    [Fact]
    public async Task GetProducts_WhenCatalogHasProducts_ReturnsPagedList()
    {
        await _steps.Given_CatalogHasProducts(count: 15);
        var response = await _steps.When_GetProductsIsCalled(page: 1, pageSize: 12);
        await _steps.Then_ResponseIs200WithItems(response, expectedCount: 12, expectedTotal: 15);
    }

    [Fact]
    public async Task GetProducts_WhenCategoryFilterApplied_ReturnsFilteredProducts()
    {
        await _steps.Given_CatalogHasProductsInCategory("Electronics", count: 5);
        var response = await _steps.When_GetProductsIsCalled(page: 1, pageSize: 12, category: "Electronics");
        await _steps.Then_ResponseIs200WithItems(response, expectedCount: 5, expectedTotal: 5);
    }
}
```

**Steps class pattern** (D-25):
```csharp
// File: GetProductsSteps.cs
public class GetProductsSteps(PostgresFixture fixture)
{
    private readonly HttpClient _client = new ServiceWebApplicationFactory(fixture.ConnectionString)
        .CreateClient();

    public async Task Given_CatalogHasProducts(int count)
    {
        // seed via EF Core directly using fixture.ConnectionString
    }

    public async Task<HttpResponseMessage> When_GetProductsIsCalled(
        int page, int pageSize, string? category = null)
    {
        var url = $"/products?page={page}&pageSize={pageSize}";
        if (category is not null) url += $"&category={category}";
        return await _client.GetAsync(url);
    }

    public async Task Then_ResponseIs200WithItems(
        HttpResponseMessage response, int expectedCount, int expectedTotal)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        body!.Items.Should().HaveCount(expectedCount);
        body.TotalCount.Should().Be(expectedTotal);
    }
}
```

**Method naming rule (D-25):** Tests class methods: `MethodName_StateUnderTest_ExpectedBehavior`. Steps methods: `Given_*()`, `When_*()`, `Then_*()`.

---

### Angular `src/frontend/ecommerce-app/` (component, request-response)

**Analog:** No TypeScript or Angular files exist in the codebase. All Angular patterns come from RESEARCH.md Pattern 8 and the UI-SPEC.md.

**`app.config.ts` pattern** (RESEARCH.md Pattern 8):
```typescript
// src/app/app.config.ts
import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideAuth, authInterceptor } from 'angular-auth-oidc-client';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor()])),
    provideAuth({
      config: {
        authority: 'http://localhost:5005',  // Identity service directly — NOT through YARP
        redirectUrl: 'http://localhost:4200/callback',
        postLogoutRedirectUri: 'http://localhost:4200',
        clientId: 'ecommerce-spa',
        scope: 'openid profile email',
        responseType: 'code',
        silentRenew: false,
        useRefreshToken: false,
        secureRoutes: ['http://localhost:4200/api'],
      },
    }),
  ],
};
```

**`proxy.conf.json` pattern:**
```json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false,
    "changeOrigin": true
  }
}
```

**Angular component skeleton** (standalone, signals, no NgModules):
```typescript
// src/app/features/catalog/catalog-list/catalog-list.component.ts
import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { HttpClient } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-catalog-list',
  standalone: true,
  imports: [MatProgressBarModule, MatChipsModule, MatPaginatorModule],
  template: `...`,
})
export class CatalogListComponent implements OnInit {
  private http = inject(HttpClient);

  products = signal<Product[]>([]);
  isLoading = signal(false);
  selectedCategory = signal<string | null>(null);
  currentPage = signal(0);
  totalCount = signal(0);

  ngOnInit() { this.loadProducts(); }

  loadProducts() {
    this.isLoading.set(true);
    const page = this.currentPage() + 1;
    const category = this.selectedCategory();
    let url = `/api/catalog/products?page=${page}&pageSize=12`;
    if (category) url += `&category=${encodeURIComponent(category)}`;

    this.http.get<PagedResult<Product>>(url).subscribe({
      next: (result) => {
        this.products.set(result.items);
        this.totalCount.set(result.totalCount);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }
}
```

**Angular rules (from CLAUDE.md + UI-SPEC.md):**
- Always use `standalone: true` — no NgModules
- Always use signals, not BehaviorSubject, for component state
- Use `@if` template control flow, not `*ngIf`
- Imports only from `@angular/material/*` secondary entry points
- No `BrowserModule`, no `CommonModule`

---

## Shared Patterns

### Serilog Bootstrap + OTel (all new .NET `Program.cs` files)

**Source:** `src/services/identity/ECommerce.Identity.API/Program.cs` (lines 1-45)
**Apply to:** All new service `Program.cs` files (Gateway, test host factories)

The three-part structure is mandatory and must not be altered:
1. Bootstrap `Log.Logger` before `CreateBuilder` (captures startup crashes)
2. `builder.Host.UseSerilog()` inside the try block
3. `Log.CloseAndFlush()` in the finally block with int return code

### .csproj Baseline (all new .NET projects)

**Source:** `src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj` (lines 1-22)
**Apply to:** All new `.csproj` files (Gateway.API, Tests.Common, test projects)

Mandatory PropertyGroup for every project:
```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

For test projects, add `<IsPackable>false</IsPackable>` and `<IsTestProject>true</IsTestProject>`.

### Solution File Structure (all new .sln files)

**Source:** `src/services/catalog/Catalog.sln` (lines 1-49)
**Apply to:** `Gateway.sln`, test project solution files

Pattern: API project + Contracts.csproj referenced via relative path `..\..\building-blocks\Contracts\Contracts.csproj`. For test `.sln` files that are siblings to the API project, the depth is one level deeper: `..\..\..\..\building-blocks\Contracts\Contracts.csproj` if test project is inside a `ECommerce.{Service}.Tests/` folder under the service root.

### IMessageEnvelope Contracts (all new event record files)

**Source:** `src/building-blocks/Contracts/IMessageEnvelope.cs` (lines 1-9) + `src/building-blocks/Contracts/Cart/Events/V1/Placeholder.cs` (lines 1-11)
**Apply to:** `CatalogSeeded.cs` and any future event records

Pattern for any new event record:
```csharp
using ECommerce.Contracts;

namespace ECommerce.{Service}.{EventsOrCommands}.V1;

public record EventName(
    Guid MessageId,        // IMessageEnvelope — always first
    Guid CorrelationId,    // IMessageEnvelope
    Guid CausationId,      // IMessageEnvelope
    DateTimeOffset OccurredAt, // IMessageEnvelope
    // domain-specific fields after envelope fields
) : IMessageEnvelope;
```

### Error Handling Pattern (all Minimal API endpoints)

**Source:** RESEARCH.md ASVS V7 + anti-patterns section
**Apply to:** All `*Endpoints.cs` files in Identity and Catalog

- Validation errors: `Results.ValidationProblem(validation.ToDictionary())`
- Not found: `Results.NotFound(new { error = "message" })`
- Conflict: `Results.Conflict(new { error = "message" })`
- Unexpected: `Results.Problem("Internal error")` — never raw exceptions
- Never: expose `Exception.Message`, stack traces, or inner exceptions

### MassTransit Version Pin (all MassTransit `.csproj` references)

**Source:** `docs/adr/0006-masstransit-outbox-inbox.md` + RESEARCH.md critical note
**Apply to:** Catalog.API.csproj, Notifications.API.csproj, all test `.csproj` files using `MassTransit.Testing`

Rule: Every MassTransit package reference must explicitly specify `Version="8.3.6"`. No floating versions. This applies to: `MassTransit`, `MassTransit.Azure.ServiceBus.Core`, `MassTransit.EntityFrameworkCore`, `MassTransit.Testing`.

### Aspire AppHost Resource Registration (AppHost Program.cs)

**Source:** `src/ecommerce.AppHost/Program.cs` (lines 1-99)
**Apply to:** Gateway resource registration

The existing pattern is `builder.AddProject<Projects.{Name}>("{logical-name}")` with explicit port, `WithExternalHttpEndpoints()`, and `WithReference(resource)`. The logical name string (e.g., `"gateway"`) must match the YARP cluster destination address.

---

## No Analog Found

Files with no close match in the codebase (planner uses RESEARCH.md patterns as primary source):

| File | Role | Data Flow | Reason | RESEARCH.md Pattern |
|------|------|-----------|--------|---------------------|
| `IdentityDbContext.cs` | model | CRUD | No EF Core code exists in project | Code Examples: "IdentityDbContext" |
| `CatalogDbContext.cs` | model | CRUD | No EF Core code exists in project | Code Examples: "CatalogDbContext with Outbox" |
| `NotificationsDbContext.cs` | model | CRUD | No EF Core code exists in project | Code Examples section |
| `DbInitializer.cs` (both) | service | batch | No IHostedService exists in project | Pattern 3 (Identity seeder) |
| `CatalogSeededConsumer.cs` | service | event-driven | No MassTransit consumers exist in project | Pattern 5 (inbox consumer) |
| `AuthorizationEndpoint.cs` | controller | request-response | No OpenIddict code exists in project | Pattern 2 (PKCE authorize endpoint) |
| `Login.cshtml` / `Login.cshtml.cs` | component/controller | request-response | No Razor Pages exist in project | Standard ASP.NET Core Identity pattern |
| `appsettings.json` (gateway) | config | — | No YARP config exists in project | Pattern 7 (YARP appsettings) |
| `PostgresFixture.cs` | utility | batch | No test infrastructure exists | Pattern 10 (Testcontainers) |
| All Angular files | component | request-response | No frontend files exist in project | Pattern 8 (app.config.ts + angular-auth-oidc-client) |
| `ProductBuilder.cs`, `UserBuilder.cs` | utility | transform | No test builders exist | CONTEXT.md D-27 specifics |

---

## Metadata

**Analog search scope:** `src/building-blocks/`, `src/services/`, `src/ecommerce.AppHost/`, `.github/workflows/`
**Files scanned:** 27 source files (excl. obj/ directories)
**Pattern extraction date:** 2026-06-17

**Key finding:** The codebase is a clean Phase 1 scaffold — every service is a uniform stub. This means:
1. The strongest analog for all `Program.cs` expansions is the existing stub (copy structure, add on top)
2. The strongest analog for all `.csproj` files is the existing service `.csproj` (copy PropertyGroup, add ItemGroups)
3. The strongest analog for all Contracts records is the existing `Placeholder.cs` pattern (primary constructor, `: IMessageEnvelope`, `using ECommerce.Contracts`)
4. The strongest analog for solution files is `Catalog.sln` (two-project: API + Contracts)
5. All EF Core, OpenIddict, MassTransit, and Angular patterns come exclusively from RESEARCH.md
