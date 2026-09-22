using System.Security.Claims;
using Google.Apis.Auth;
using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleanBase.Application.Identity.Authentication.Services;

public class GoogleAuthService(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IJwtService jwtService,
    IClaimsProvider claimsProvider,
    IRefreshTokenService refreshTokenService,
    IUserRegistrationService userRegistrationService,
    IConfiguration configuration,
    ILogger<GoogleAuthService> logger
)
    : IGoogleAuthService
{
    public async Task<ErrorOr<AuthTokenResult>> HandleCallbackAsync()
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
            return Errors.Identity.ExternalInfoMissing;

        var signInResult = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        AppUser? user;
        if (!signInResult.Succeeded)
        {
            var userEmail = info.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
            var existingUserByEmail = await userManager.FindByEmailAsync(userEmail);

            if (existingUserByEmail is not null)
            {
                existingUserByEmail.EmailConfirmed = true;
                await userManager.UpdateAsync(existingUserByEmail);
                user = existingUserByEmail;
                await userManager.AddLoginAsync(user, info);
            }
            else
            {
                var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
                var surname = info.Principal.FindFirstValue(ClaimTypes.Surname);
                var fullName = info.Principal.FindFirstValue(ClaimTypes.Name);
                var picture = info.Principal.FindFirstValue("picture");

                var firstName = givenName ?? fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                var lastName = surname ?? (fullName?.Contains(' ') == true
                    ? string.Join(' ', fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1))
                    : "");

                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrWhiteSpace(email))
                    return Errors.Identity.InvalidGoogleIdToken;

                var registrationResult = await userRegistrationService.RegisterAsync(new RegisterUserRequest(
                    Email: email,
                    FirstName: firstName,
                    LastName: lastName,
                    ProfilePictureUrl: picture,
                    ExternalLogin: info,
                    EmailConfirmed: true));

                if (registrationResult.IsError)
                    return Errors.Identity.RegistrationFailed($"Failed to register user: {string.Join(", ", registrationResult.Errors.Select(e => e.Description))}");

                user = registrationResult.Value;
            }
        }
        else
        {
            user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        }

        if (user is null)
            return Errors.Identity.UserNotFound;

        if (user.IsPendingDeletion)
            return Errors.Identity.AccountPendingDeletion;

        var claims = await claimsProvider.GetClaimsAsync(user.Id);
        var jwtResult = jwtService.GenerateToken(claims);

        var refreshTokenResult = await refreshTokenService.GenerateRefreshTokenAsync(user.Id);
        if (refreshTokenResult.IsError)
            return refreshTokenResult.Errors;

        return new AuthTokenResult(jwtResult.Token, refreshTokenResult.Value.Token, jwtResult.ExpiresAt);
    }

    public async Task<ErrorOr<AuthTokenResult>> HandleMobileLoginAsync(string idToken)
    {
        try
        {
            var googleClientId = configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(googleClientId))
            {
                logger.LogWarning("Google Client ID is not configured.");
                throw new InvalidOperationException("Google Client ID is not configured.");
            }

            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [googleClientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
            if (payload == null)
                return Errors.Identity.InvalidGoogleIdToken;

            var user = await userManager.FindByLoginAsync("Google", payload.Subject);

            if (user is null)
            {
                var existingUserByEmail = await userManager.FindByEmailAsync(payload.Email);
                if (existingUserByEmail is not null)
                {
                    existingUserByEmail.EmailConfirmed = true;
                    await userManager.UpdateAsync(existingUserByEmail);
                    user = existingUserByEmail;
                    await userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
                }
                else
                {
                    var firstName = payload.GivenName ??
                                    payload.Name?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                    var lastName = payload.FamilyName ?? (payload.Name?.Contains(' ') == true
                        ? string.Join(' ', payload.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1))
                        : "");

                    var registrationResult = await userRegistrationService.RegisterAsync(new RegisterUserRequest(
                        Email: payload.Email,
                        FirstName: firstName,
                        LastName: lastName,
                        ProfilePictureUrl: payload.Picture,
                        ExternalLogin: new UserLoginInfo("Google", payload.Subject, "Google"),
                        EmailConfirmed: true));

                    if (registrationResult.IsError)
                        return Errors.Identity.RegistrationFailed($"Failed to register user: {string.Join(", ", registrationResult.Errors.Select(e => e.Description))}");

                    user = registrationResult.Value;
                }
            }

            if (user.IsPendingDeletion)
                return Errors.Identity.AccountPendingDeletion;

            var claims = await claimsProvider.GetClaimsAsync(user.Id);
            var jwtResult = jwtService.GenerateToken(claims);

            var refreshTokenResult = await refreshTokenService.GenerateRefreshTokenAsync(user.Id);
            if (refreshTokenResult.IsError)
                return refreshTokenResult.Errors;

            return new AuthTokenResult(jwtResult.Token, refreshTokenResult.Value.Token, jwtResult.ExpiresAt);
        }
        catch (Exception e)
        {
            logger.LogWarning("An Exception during Google ID token validation. {Message}", e.Message);
            return Errors.Identity.UnexpectedGoogleIdTokenError;
        }
    }
}
