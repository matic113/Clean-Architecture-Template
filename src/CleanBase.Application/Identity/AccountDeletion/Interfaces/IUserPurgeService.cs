namespace CleanBase.Application.Identity.AccountDeletion.Interfaces;

public interface IUserPurgeService
{
    /// <summary>
    /// Permanently deletes every account whose retention window has elapsed — external assets
    /// (R2 audio/avatars, Cartesia clones) first, then the user row (DB cascade removes the rest).
    /// Runs as a daily Hangfire recurring job.
    /// </summary>
    Task PurgeExpiredAsync(CancellationToken ct = default);
}
