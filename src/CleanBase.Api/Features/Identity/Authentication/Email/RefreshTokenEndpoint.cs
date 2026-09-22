using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;

namespace CleanBase.Api.Features.Identity.Authentication.Email;

public sealed record RefreshTokenRequestDto
{
    public string RefreshToken { get; init; } = string.Empty;

    public class Validator : Validator<RefreshTokenRequestDto>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required.");
        }
    }
}

public sealed class RefreshTokenEndpoint(IIdentityService identityService)
    : Endpoint<RefreshTokenRequestDto, AuthTokenResult>
{
    public override void Configure()
    {
        Post("/auth/refresh-token");
        AllowAnonymous();
        Description(b => b
            .WithDescription("Refreshes the JWT access token using a valid refresh token.")
            .WithTags("Auth"));
    }

    public override async Task HandleAsync(RefreshTokenRequestDto req, CancellationToken ct)
    {
        var result = await identityService.RefreshTokenAsync(req.RefreshToken);

        await Send.OkAsync(result, ct);
    }
}