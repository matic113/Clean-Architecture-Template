using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Domain.Identity;

namespace CleanBase.Api.Features.Identity.Authentication.Email;

public class VerifyEmail
{
    public sealed record VerifyEmailRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;

        public class Validator : Validator<VerifyEmailRequestDto>
        {
            public Validator()
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Please provide a valid email address.");

                RuleFor(x => x.Code)
                    .NotEmpty().WithMessage("Verification code is required.")
                    .Length(6).WithMessage("Verification code must be 6 digits.")
                    .Matches("^[0-9]+$").WithMessage("Verification code must contain only digits.");
            }
        }
    }

    public sealed record SuccessResponse(string Message);

    public sealed class VerifyEmailEndpoint(IIdentityService identityService)
        : Endpoint<VerifyEmailRequestDto, AuthTokenResult>
    {
        public override void Configure()
        {
            Post("/auth/verify-email");
            AllowAnonymous();
            Description(b => b
                .WithDescription("Verifies the user's email address using the OTP code.")
                .WithTags("Auth"));
        }

        public override async Task HandleAsync(VerifyEmailRequestDto req, CancellationToken ct)
        {
            var request = new VerifyOtpRequest
            {
                Email = req.Email,
                Code = req.Code,
                Purpose = OtpPurpose.EmailVerification
            };

            var result = await identityService.VerifyEmailOtpAsync(request);

            await Send.OkAsync(result, ct);
        }
    }
}
