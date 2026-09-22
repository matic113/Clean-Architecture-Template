using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Application.Identity.Authentication.Options;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CleanBase.Application.Identity.Authentication.Services;

public class AppleAuthService(
    UserManager<AppUser> userManager,
    IClaimsProvider claimsProvider,
    IJwtService jwtService,
    IRefreshTokenService refreshTokenService,
    IUserRegistrationService userRegistrationService,
    IOptions<AppleAuthOptions> appleAuthOptions,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    ILogger<AppleAuthService> logger
)
    : IAppleAuthService
{
    private const string AppleProvider = "Apple";
    private const string AppleIssuer = "https://appleid.apple.com";
    private const string AppleJwksUrl = "https://appleid.apple.com/auth/keys";
    private const string AppleJwksCacheKey = "AppleAuth_JWKS";
    private static readonly TimeSpan JwksCacheDuration = TimeSpan.FromHours(1);

    public async Task<ErrorOr<AuthTokenResult>> HandleMobileLoginAsync(string idToken)
    {
        try
        {
            var payloadResult = await ValidateTokenAsync(idToken);
            if (payloadResult.IsError)
                return payloadResult.Errors;

            var payload = payloadResult.Value;
            var email = payload.Email;

            var user = await userManager.FindByLoginAsync(AppleProvider, payload.Subject);

            if (user is null)
            {
                if (string.IsNullOrWhiteSpace(email))
                    return Errors.Identity.AppleEmailNotProvided;

                var existingUserByEmail = await userManager.FindByEmailAsync(email);
                if (existingUserByEmail is not null)
                {
                    existingUserByEmail.EmailConfirmed = true;
                    await userManager.UpdateAsync(existingUserByEmail);
                    user = existingUserByEmail;

                    var linkResult = await userManager.AddLoginAsync(
                        user,
                        new UserLoginInfo(AppleProvider, payload.Subject, AppleProvider));

                    if (!linkResult.Succeeded)
                        return Errors.Identity.UnexpectedAppleIdTokenError;
                }
                else
                {
                    var registrationResult = await userRegistrationService.RegisterAsync(new RegisterUserRequest(
                        Email: email,
                        ExternalLogin: new UserLoginInfo(AppleProvider, payload.Subject, AppleProvider),
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
            logger.LogWarning(e, "An exception occurred during Apple ID token validation. {Message}", e.Message);
            return Errors.Identity.UnexpectedAppleIdTokenError;
        }
    }

    private async Task<ErrorOr<AppleIdTokenPayload>> ValidateTokenAsync(string idToken)
    {
        var bundleIds = appleAuthOptions.Value.BundleIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();

        if (bundleIds.Length == 0)
            return Errors.Identity.UnexpectedAppleIdTokenError;

        try
        {
            var jwks = await GetJwksAsync();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = AppleIssuer,
                ValidateAudience = true,
                ValidAudiences = bundleIds,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                IssuerSigningKeys = jwks.Keys,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(idToken, validationParameters, out _);

            var email = principal.FindFirstValue(ClaimTypes.Email)?.Trim().ToLowerInvariant();
            var emailVerified = principal.FindFirstValue("email_verified");

            if (emailVerified != "true")
                return Errors.Identity.AppleEmailNotVerified;

            var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(subject))
                return Errors.Identity.InvalidAppleIdToken;

            return new AppleIdTokenPayload(subject, email);
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return Errors.Identity.InvalidAppleIdToken;
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return Errors.Identity.InvalidAppleIdToken;
        }
        catch (SecurityTokenValidationException)
        {
            return Errors.Identity.InvalidAppleIdToken;
        }
        catch
        {
            return Errors.Identity.UnexpectedAppleIdTokenError;
        }
    }

    private async Task<JsonWebKeySet> GetJwksAsync()
    {
        if (cache.TryGetValue(AppleJwksCacheKey, out JsonWebKeySet? cached) && cached is not null)
            return cached;

        var jwksJson = await httpClientFactory
            .CreateClient("AppleJwks")
            .GetStringAsync(AppleJwksUrl);

        var jwks = new JsonWebKeySet(jwksJson);
        cache.Set(AppleJwksCacheKey, jwks, JwksCacheDuration);

        return jwks;
    }

    private sealed record AppleIdTokenPayload(string Subject, string? Email);
}
