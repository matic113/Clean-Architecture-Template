using CleanBase.Application.Identity.AccountDeletion.DTOs;
using CleanBase.Application.Identity.AccountDeletion.Interfaces;

namespace CleanBase.Api.Features.Identity.AccountDeletion;

public class RequestDeletion
{
    public sealed record RequestDeletionDto
    {
        public string Email { get; init; } = string.Empty;

        public class Validator : Validator<RequestDeletionDto>
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

    public sealed class RequestDeletionEndpoint(IAccountDeletionService accountDeletionService)
        : Endpoint<RequestDeletionDto, SuccessResponse>
    {
        public override void Configure()
        {
            Post("/account/deletion/request");
            AllowAnonymous();
            Description(b => b
                .WithDescription("Sends an email OTP to confirm self-service account deletion.")
                .WithTags("Account Deletion"));
        }

        public override async Task HandleAsync(RequestDeletionDto req, CancellationToken ct)
        {
            var request = new RequestAccountDeletionRequest { Email = req.Email };

            var result = await accountDeletionService.RequestDeletionAsync(request);

            // Deliberately generic so the response never reveals whether the email has an account.
            await Send.OkAsync(result,
                _ => new SuccessResponse("If an account exists for this email, a deletion code has been sent."),
                ct);
        }
    }
}
