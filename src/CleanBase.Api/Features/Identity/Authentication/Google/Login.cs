using CleanBase.Application.Identity.Authentication.Options;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CleanBase.Api.Features.Identity.Authentication.Google;

public class Login
{
    sealed class GoogleLoginRequest
    {
        [QueryParam] public required string RedirectUri { get; set; }

        public class Validator : Validator<GoogleLoginRequest>
        {
            public Validator()
            {
                RuleFor(x => x.RedirectUri)
                    .NotEmpty()
                    .WithMessage("RedirectUri is required.");
            }
        }
    };

    sealed class GoogleLoginEndpoint(IOptions<AuthOptions> authOptions, SignInManager<AppUser> signInManager)
        : Endpoint<GoogleLoginRequest>
    {
        public override void Configure()
        {
            Get("/auth/google/login");
            AllowAnonymous();
            DontAutoSendResponse();
            Description(b => b
                .WithDescription("Initiates Google OAuth2 login process.")
                .WithTags("Auth/Google"));
        }

        public override async Task HandleAsync(GoogleLoginRequest r, CancellationToken c)
        {
            var props = signInManager.ConfigureExternalAuthenticationProperties(
                GoogleDefaults.AuthenticationScheme,
                "/api/auth/google/callback"
            );

            if (!Uri.TryCreate(r.RedirectUri, UriKind.Absolute, out var redirectUri))
            {
                ThrowError("Invalid RedirectUri.");
                await Send.ErrorsAsync(cancellation: c);
            }

            if (!authOptions.Value.IsRedirectUriAllowed(r.RedirectUri))
            {
                ThrowError("The provided RedirectUri is not allowed.");
                await Send.ErrorsAsync(cancellation: c);
            }

            // Store RedirectUri in authentication properties so we can retrieve it in callback
            props.Items["RedirectUri"] = r.RedirectUri;

            await HttpContext.ChallengeAsync(
                GoogleDefaults.AuthenticationScheme,
                props);
        }
    }
}
