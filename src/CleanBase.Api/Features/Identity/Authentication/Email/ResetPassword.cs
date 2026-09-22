using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;

namespace CleanBase.Api.Features.Identity.Authentication.Email;

public class ResetPassword
{
    public sealed record ResetPasswordRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;

        public class Validator : Validator<ResetPasswordRequestDto>
        {
            public Validator()
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Please provide a valid email address.");

                RuleFor(x => x.Code)
                    .NotEmpty().WithMessage("Reset code is required.")
                    .Length(6).WithMessage("Reset code must be 6 digits.")
                    .Matches("^[0-9]+$").WithMessage("Reset code must contain only digits.");

                RuleFor(x => x.NewPassword)
                    .NotEmpty().WithMessage("New password is required.")
                    .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
            }
        }
    }

    public sealed record SuccessResponse(string Message);

    public sealed class ResetPasswordEndpoint(IIdentityService identityService)
        : Endpoint<ResetPasswordRequestDto, SuccessResponse>
    {
        public override void Configure()
        {
            Post("/auth/reset-password");
            AllowAnonymous();
            Description(b => b
                .WithDescription("Resets the user's password using a valid OTP and the new password.")
                .WithTags("Auth"));
        }

        public override async Task HandleAsync(ResetPasswordRequestDto req, CancellationToken ct)
        {
            var request = new ResetPasswordRequest
            {
                Email = req.Email,
                Code = req.Code,
                NewPassword = req.NewPassword
            };

            var result = await identityService.ResetPasswordWithOtpAsync(request);

            await Send.OkAsync(result,
                _ => new SuccessResponse("Password reset successfully! You can now log in with your new password."),
                ct);
        }
    }
}
