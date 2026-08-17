using System.Security.Claims;
using ECommerce.Notifications.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Notifications.API.Features.Notifications;

public static class NotificationsEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/notifications", async (
            ClaimsPrincipal user,
            NotificationsDbContext db,
            CancellationToken ct) =>
        {
            var userId = GetUserId(user);

            var items = await db.NotificationEntries
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.OccurredAt)
                .ToListAsync(ct);

            return Results.Ok(items);
        }).RequireAuthorization();
    }

    private static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Authenticated request missing a user id claim.");
}
