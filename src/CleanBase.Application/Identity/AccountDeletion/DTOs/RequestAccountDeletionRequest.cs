namespace CleanBase.Application.Identity.AccountDeletion.DTOs;

public record RequestAccountDeletionRequest
{
    public required string Email { get; init; }
}
