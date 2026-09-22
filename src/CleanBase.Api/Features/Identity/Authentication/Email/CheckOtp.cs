using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Domain.Identity;

namespace CleanBase.Api.Features.Identity.Authentication.Email;

public class CheckOtp
{
    public sealed record CheckOtpRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string? Purpose { get; init; }

        public class Validator : Validator<CheckOtpRequestDto>
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

                RuleFor(x => x.Purpose)
                    .NotEmpty().WithMessage("OTP purpose is required.")
                    .IsEnumName(typeof(OtpPurpose), caseSensitive: false)
                    .WithMessage("OTP purpose is invalid. Valid values are: EmailVerification, PasswordReset.");
            }
        }
    }

    public sealed record SuccessResponse(string Message);

    public sealed class CheckOtpEndpoint(IIdentityService identityService)
        : Endpoint<CheckOtpRequestDto, SuccessResponse>
    {
        public override void Configure()
        {
            Post("/auth/check-otp");
            AllowAnonymous();
            Description(b => b
                .WithDescription("Verifies an OTP code without consuming it.")
                .WithTags("Auth"));
        }

        public override async Task HandleAsync(CheckOtpRequestDto req, CancellationToken ct)
        {
            var request = new VerifyOtpRequest
            {
                Email = req.Email,
                Code = req.Code,
                Purpose = Enum.Parse<OtpPurpose>(req.Purpose!, ignoreCase: true)
            };

            var result = await identityService.CheckOtpAsync(request);

            await Send.OkAsync(result, _ => new SuccessResponse("Code is valid."), ct);
        }
    }
}
