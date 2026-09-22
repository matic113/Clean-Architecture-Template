using CleanBase.Application.Identity.AccountDeletion.DTOs;
using CleanBase.Application.Identity.AccountDeletion.Interfaces;

namespace CleanBase.Api.Features.Identity.AccountDeletion;

public class ConfirmDeletion
{
    public sealed record ConfirmDeletionDto
    {
        public string Email { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;

        public class Validator : Validator<ConfirmDeletionDto>
        {
            public Validator()
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Please provide a valid email address.");

                RuleFor(x => x.Code)
                    .NotEmpty().WithMessage("Confirmation code is required.")
                    .Length(6).WithMessage("Confirmation code must be 6 digits.")
                    .Matches("^[0-9]+$").WithMessage("Confirmation code must contain only digits.");
            }
        }
    }

    public sealed record ConfirmDeletionResponse(string Message, DateTime PurgeScheduledAt);

    public sealed class ConfirmDeletionEndpoint(IAccountDeletionService accountDeletionService)
        : Endpoint<ConfirmDeletionDto, ConfirmDeletionResponse>
    {
        public override void Configure()
        {
            Post("/account/deletion/confirm");
            AllowAnonymous();
            Description(b => b
                .WithDescription("Confirms account deletion with the emailed OTP and locks the account for 30 days.")
                .WithTags("Account Deletion"));
        }

        public override async Task HandleAsync(ConfirmDeletionDto req, CancellationToken ct)
        {
            var request = new ConfirmAccountDeletionRequest { Email = req.Email, Code = req.Code };

            var result = await accountDeletionService.ConfirmDeletionAsync(request);

            await Send.OkAsync(result,
                r => new ConfirmDeletionResponse(
                    "Your account is scheduled for deletion in 30 days and has been signed out on all devices. "
                    + "Contact support within 30 days to restore it. "
                    + "Deleting your account does not cancel an Apple App Store or Google Play subscription — "
                    + "please cancel that separately in your store account.",
                    r.PurgeScheduledAt),
                ct);
        }
    }
}
