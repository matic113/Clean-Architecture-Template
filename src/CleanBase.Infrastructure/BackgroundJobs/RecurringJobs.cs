using Hangfire;
using CleanBase.Application.Identity.AccountDeletion.Interfaces;
using Microsoft.AspNetCore.Builder;

namespace CleanBase.Infrastructure.BackgroundJobs;

public static class RecurringJobs
{
    /// <summary>Registers the app's recurring Hangfire jobs (idempotent — safe on every startup).</summary>
    public static void UseRecurringJobs(this WebApplication app)
    {
        // Permanently erase accounts whose 30-day retention window has elapsed.
        RecurringJob.AddOrUpdate<IUserPurgeService>(
            "purge-pending-deletion-users",
            svc => svc.PurgeExpiredAsync(CancellationToken.None),
            Cron.Daily);
    }
}
