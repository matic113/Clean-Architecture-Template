using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Domain.Identity;

namespace CleanBase.Api.Features.Identity.Authentication.Email;

public class ResendOtp
{
    public sealed record ResendOtpRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string? Purpose { get; init; }

        public class Validator : Validator<ResendOtpRequestDto>
        {
            public Validator()
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Please provide a valid email address.");

                RuleFor(x => x.Purpose)
                    .NotEmpty().WithMessage("OTP purpose is required.")
                    .IsEnumName(typeof(OtpPurpose), caseSensitive: false)
                    .WithMessage("OTP purpose is invalid. Valid values are: EmailVerification, PasswordReset.");
            }
        }
    }

    public sealed record SuccessResponse(string Message);

    public sealed class ResendOtpEndpoint(IIdentityService identityService)
        : Endpoint<ResendOtpRequestDto, SuccessResponse>
    {
        public override void Configure()
        {
            Post("/auth/resend-otp");
            AllowAnonymous();
            Description(b => b
                .WithDescription("Resends the OTP for email verification or password reset.")
                .WithTags("Auth"));
        }

        public override async Task HandleAsync(ResendOtpRequestDto req, CancellationToken ct)
        {
            var request = new ResendOtpRequest
            {
                Email = req.Email,
                Purpose = Enum.Parse<OtpPurpose>(req.Purpose!, ignoreCase: true)
            };

            var result = await identityService.ResendOtpAsync(request);

            await Send.OkAsync(result,
                _ => new SuccessResponse("Verification code sent! Please check your email."), ct);
        }
    }
}
