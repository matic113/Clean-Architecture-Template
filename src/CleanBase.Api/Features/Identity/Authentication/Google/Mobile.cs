using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;

namespace CleanBase.Api.Features.Identity.Authentication.Google;

public class Mobile
{
    public sealed record GoogleMobileLoginRequestDto
    {
        public string IdToken { get; init; } = string.Empty;

        public class Validator : Validator<GoogleMobileLoginRequestDto>
        {
            public Validator()
            {
                RuleFor(x => x.IdToken)
                    .NotEmpty().WithMessage("ID token is required.")
                    .MinimumLength(50).WithMessage("ID token appears to be invalid.");
            }
        }
    }

    public sealed class GoogleMobileLoginEndpoint(IGoogleAuthService googleAuthService)
        : Endpoint<GoogleMobileLoginRequestDto, AuthTokenResult>
    {
        public override void Configure()
        {
            Post("/auth/google/mobile");
            AllowAnonymous();
            Description(b => b
                .WithDescription("Authenticates a user using a Google ID token from a mobile device.")
                .WithTags("Auth/Google"));
        }

        public override async Task HandleAsync(GoogleMobileLoginRequestDto req, CancellationToken ct)
        {
            var result = await googleAuthService.HandleMobileLoginAsync(req.IdToken);

            await Send.OkAsync(result, ct);
        }
    }
}
