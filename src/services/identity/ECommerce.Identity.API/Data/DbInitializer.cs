using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ECommerce.Identity.API.Data;

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
                ClientType = ClientTypes.Public,
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
        await SeedUserIfNotExists(userManager, "demo@example.com", "Demo123!", ct);
        await SeedUserIfNotExists(userManager, "admin@example.com", "Admin123!", ct);
    }

    private static async Task SeedUserIfNotExists(
        UserManager<IdentityUser> um,
        string email,
        string password,
        CancellationToken ct)
    {
        if (await um.FindByEmailAsync(email) is null)
        {
            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var result = await um.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to seed user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
