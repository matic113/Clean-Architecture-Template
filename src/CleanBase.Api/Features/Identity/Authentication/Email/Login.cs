using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;

namespace CleanBase.Api.Features.Identity.Authentication.Email;

public class Login
{
    public sealed record LoginRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;

        public class Validator : Validator<LoginRequestDto>
        {
            public Validator()
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Please provide a valid email address.");

                RuleFor(x => x.Password)
                    .NotEmpty().WithMessage("Password is required.");
            }
        }
    }

    public sealed class LoginEndpoint(IIdentityService identityService)
        : Endpoint<LoginRequestDto, AuthTokenResult>
    {
        public override void Configure()
        {
            Post("/auth/login");
            AllowAnonymous();
            Description(b => b
                .WithDescription("Logs in a user using their email and password.")
                .WithTags("Auth"));
        }

        public override async Task HandleAsync(LoginRequestDto req, CancellationToken ct)
        {
            var request = new LoginRequest
            {
                Email = req.Email,
                Password = req.Password
            };

            var result = await identityService.LoginWithEmailAsync(request);

            await Send.OkAsync(result, ct);
        }
    }
}
