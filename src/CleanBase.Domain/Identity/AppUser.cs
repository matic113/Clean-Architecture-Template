using Microsoft.AspNetCore.Identity;

namespace CleanBase.Domain.Identity;

public class AppUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// When the user confirmed a self-service account deletion (via emailed OTP). Non-null means the
    /// account is locked in the reversible retention window; <see cref="PurgeScheduledAt"/> is when the
    /// daily job hard-deletes it. Both are cleared when an admin restores the account.
    /// </summary>
    public DateTime? DeletionRequestedAt { get; set; }

    /// <summary>The earliest time the purge job may permanently erase this user (request + retention window).</summary>
    public DateTime? PurgeScheduledAt { get; set; }

    /// <summary>True while the account is in the 30-day pending-deletion window (locked, restorable by an admin).</summary>
    public bool IsPendingDeletion => DeletionRequestedAt is not null;

    /// <summary>Flags the account for deletion after <paramref name="retentionDays"/> days.</summary>
    public void RequestDeletion(int retentionDays)
    {
        var now = DateTime.UtcNow;
        DeletionRequestedAt = now;
        PurgeScheduledAt = now.AddDays(retentionDays);
    }

    /// <summary>Cancels a pending deletion (admin restore).</summary>
    public void CancelDeletion()
    {
        DeletionRequestedAt = null;
        PurgeScheduledAt = null;
    }
}
