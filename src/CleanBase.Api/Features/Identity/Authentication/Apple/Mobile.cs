using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;

namespace CleanBase.Api.Features.Identity.Authentication.Apple;

public class Mobile
{
    public sealed record AppleMobileLoginRequestDto
    {
        public string IdToken { get; init; } = string.Empty;

        public class Validator : Validator<AppleMobileLoginRequestDto>
        {
            public Validator()
            {
                RuleFor(x => x.IdToken)
                    .NotEmpty().WithMessage("ID token is required.")
                    .MinimumLength(50).WithMessage("ID token appears to be invalid.");
            }
        }
    }

    public sealed class AppleMobileLoginEndpoint(IAppleAuthService appleAuthService)
        : Endpoint<AppleMobileLoginRequestDto, AuthTokenResult>
    {
        public override void Configure()
        {
            Post("/auth/apple/mobile");
            AllowAnonymous();
            Description(b => b
                .WithDescription("Authenticates a user using an Apple ID token from a mobile device.")
                .WithTags("Auth/Apple"));
        }

        public override async Task HandleAsync(AppleMobileLoginRequestDto req, CancellationToken ct)
        {
            var result = await appleAuthService.HandleMobileLoginAsync(req.IdToken);

            await Send.OkAsync(result, ct);
        }
    }
}
