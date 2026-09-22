using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Application.Common.Constants;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CleanBase.Application.Identity.Authentication.Services;

public class UserRegistrationService(
    UserManager<AppUser> userManager,
    ILogger<UserRegistrationService> logger
)
    : IUserRegistrationService
{
    public async Task<ErrorOr<AppUser>> RegisterAsync(RegisterUserRequest request)
    {
        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = request.EmailConfirmed,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            ProfilePictureUrl = request.ProfilePictureUrl
        };

        IdentityResult createResult;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            createResult = await userManager.CreateAsync(user, request.Password);
        }
        else
        {
            createResult = await userManager.CreateAsync(user);
        }

        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();
            return errors;
        }

        try
        {
            if (request.ExternalLogin is not null)
            {
                var loginResult = await userManager.AddLoginAsync(user, request.ExternalLogin);
                if (!loginResult.Succeeded)
                {
                    logger.LogWarning(
                        "Failed to add external login {Provider} for user {Email}: {Errors}",
                        request.ExternalLogin.LoginProvider,
                        request.Email,
                        string.Join(", ", loginResult.Errors.Select(e => e.Description)));

                    await userManager.DeleteAsync(user);
                    return Errors.Identity.RegistrationFailed("Failed to add external login. Please try again.");
                }
            }

            var roleResult = await userManager.AddToRoleAsync(user, AppRoles.User);
            if (!roleResult.Succeeded)
            {
                logger.LogWarning(
                    "Failed to add user {Email} to role {Role}: {Errors}",
                    request.Email,
                    AppRoles.User,
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                await userManager.DeleteAsync(user);
                return Errors.Identity.RegistrationFailed("Failed to add user to role. Please try again.");
            }

            return user;
        }
        catch
        {
            await userManager.DeleteAsync(user);
            throw;
        }
    }
}
