using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;

namespace CleanBase.Api.Features.Identity.Authentication.Email;

public class ForgotPassword
{
    public sealed record ForgotPasswordRequestDto
    {
        public string Email { get; init; } = string.Empty;

        public class Validator : Validator<ForgotPasswordRequestDto>
        {
            public Validator()
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Please provide a valid email address.");
            }
        }
    }

    public sealed record SuccessResponse(string Message);

    public sealed class ForgotPasswordEndpoint(IIdentityService identityService)
        : Endpoint<ForgotPasswordRequestDto, SuccessResponse>
    {
        public override void Configure()
        {
            Post("/auth/forgot-password");
            AllowAnonymous();
            Description(b => b
                .WithDescription("Initiates the password reset process by sending an OTP to the user's email.")
                .WithTags("Auth"));
        }

        public override async Task HandleAsync(ForgotPasswordRequestDto req, CancellationToken ct)
        {
            var request = new ForgotPasswordRequest
            {
                Email = req.Email
            };

            var result = await identityService.ForgotPasswordAsync(request);

            await Send.OkAsync(result,
                _ => new SuccessResponse("Password reset code sent! Please check your email."), ct);
        }
    }
}
