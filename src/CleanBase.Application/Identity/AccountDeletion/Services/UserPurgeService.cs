using CleanBase.Application.Identity.AccountDeletion.Interfaces;
using CleanBase.Application.Utilities.Storage;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanBase.Application.Identity.AccountDeletion.Services;

public class UserPurgeService(
    UserManager<AppUser> userManager,
    IStorageService storage,
    ILogger<UserPurgeService> logger
) : IUserPurgeService
{
    public async Task PurgeExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var due = await userManager.Users
            .Where(u => u.PurgeScheduledAt != null && u.PurgeScheduledAt <= now)
            .ToListAsync(ct);

        if (due.Count == 0)
            return;

        logger.LogInformation("Purging {Count} account(s) past their retention window", due.Count);

        foreach (var user in due)
        {
            try
            {
                // External assets first — if the DB row goes and this failed, the keys would be lost.
                await PurgeExternalAssetsAsync(user);

                // Deleting the user row cascades to all owned data (refresh tokens, Identity satellite rows).
                var result = await userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to delete user {UserId}: {Errors}", user.Id,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                logger.LogInformation("Purged account {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                // Never let one bad account abort the whole sweep; it retries on the next daily run.
                logger.LogError(ex, "Unexpected error purging account {UserId}", user.Id);
            }
        }
    }

    private async Task PurgeExternalAssetsAsync(AppUser user)
    {
        // Avatar: only S3-hosted keys (private/...) — never an external OAuth picture URL.
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl)
            && user.ProfilePictureUrl.StartsWith(StorageKeys.PrivatePrefix + "/", StringComparison.Ordinal))
        {
            await TryDeleteObject(user.ProfilePictureUrl);
        }
    }

    private Task TryDeleteObject(string key)
        => TryAsync(() => storage.DeleteAsync(key), $"storage object {key}");

    private async Task TryAsync(Func<Task> action, string what)
    {
        try { await action(); }
        catch (Exception ex) { logger.LogWarning(ex, "Best-effort cleanup failed for {What}", what); }
    }
}
