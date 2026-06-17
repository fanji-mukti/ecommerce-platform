using System.Security.Claims;

namespace ECommerce.Identity.API.Features.Profile;

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
