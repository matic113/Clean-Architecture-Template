namespace CleanBase.Application.Identity.AccountDeletion.DTOs;

public record ConfirmAccountDeletionRequest
{
    public required string Email { get; init; }
    public required string Code { get; init; }
}
