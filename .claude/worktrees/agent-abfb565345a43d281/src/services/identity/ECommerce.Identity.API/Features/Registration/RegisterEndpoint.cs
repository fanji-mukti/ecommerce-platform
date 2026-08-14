using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Identity.API.Features.Registration;

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

        var user = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            // 409 for duplicate email; 400 for other failures
            var isDuplicate = result.Errors.Any(e => e.Code == "DuplicateUserName");
            return isDuplicate
                ? Results.Conflict(new { error = "Email already in use." })
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        return Results.Created("/me", new { email = request.Email });
    }
}
