namespace CleanBase.Application.Identity.AccountDeletion.DTOs;

/// <summary>An account currently in the pending-deletion retention window (admin view).</summary>
public record PendingDeletionResponse
{
    public required Guid Id { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateTime? DeletionRequestedAt { get; init; }
    public DateTime? PurgeScheduledAt { get; init; }
}
