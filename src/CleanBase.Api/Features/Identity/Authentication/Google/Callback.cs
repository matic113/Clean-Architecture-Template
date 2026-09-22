using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace CleanBase.Api.Features.Identity.Authentication.Google;

public class Callback
{
    sealed class GoogleCallbackEndpoint(SignInManager<AppUser> signInManager, IGoogleAuthService googleAuthService)
        : EndpointWithoutRequest<AuthTokenResult>
    {
        public override void Configure()
        {
            Get("auth/google/callback");
            AllowAnonymous();
            DontAutoSendResponse();
            Description(b => b
                .WithDescription("Handles the callback from Google OAuth2 login.")
                .WithTags("Auth/Google"));
        }

        public override async Task HandleAsync(CancellationToken c)
        {
            var info = await signInManager.GetExternalLoginInfoAsync();

            if (info?.AuthenticationProperties?.Items.TryGetValue("RedirectUri", out var redirectUri) != true
                || string.IsNullOrWhiteSpace(redirectUri))
            {
                ThrowError("RedirectUri not found");
                await Send.ErrorsAsync(cancellation: c);
                return;
            }

            var result = await googleAuthService.HandleCallbackAsync();

            if (result.IsError)
            {
                await Send.OkAsync(result, c);
                return;
            }

            var tokenData = result.Value;

            var accessToken = Uri.EscapeDataString(tokenData.AccessToken);
            var refreshToken = Uri.EscapeDataString(tokenData.RefreshToken);
            var expiresAt = Uri.EscapeDataString(tokenData.ExpiresAt.ToString("O"));

            // Redirect to frontend with token in URL fragment
            var redirectUrl =
                $"{redirectUri}#access_token={accessToken}&expires_at={expiresAt}&refresh_token={refreshToken}";

            HttpContext.Response.Redirect(redirectUrl);
        }
    }
}
