using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ECommerce.Identity.API.Features.Authorization;

public static class AuthorizationEndpoint
{
    public static async Task<IResult> Authorize(HttpContext ctx)
    {
        var request = ctx.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict request missing.");

        // Check if the user is already authenticated via Identity cookie
        var result = await ctx.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!result.Succeeded)
        {
            // Redirect to the login page, preserving the current path as returnUrl
            var redirectUri = QueryHelpers.AddQueryString(
                "/Account/Login",
                new Dictionary<string, string?>
                {
                    ["returnUrl"] = ctx.Request.PathBase + ctx.Request.Path + ctx.Request.QueryString
                });

            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = redirectUri },
                [IdentityConstants.ApplicationScheme]);
        }

        var user = result.Principal;

        // Build claims principal for OpenIddict token issuance
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // Subject claim — goes to both access token and identity token
        var subjectClaim = new Claim(Claims.Subject, user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        subjectClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
        identity.AddClaim(subjectClaim);

        // Email claim — identity token only
        var emailValue = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var emailClaim = new Claim(Claims.Email, emailValue);
        emailClaim.SetDestinations(Destinations.IdentityToken);
        identity.AddClaim(emailClaim);

        // Name claim — identity token only
        var nameValue = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var nameClaim = new Claim(Claims.Name, nameValue);
        nameClaim.SetDestinations(Destinations.IdentityToken);
        identity.AddClaim(nameClaim);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        return Results.SignIn(principal,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
