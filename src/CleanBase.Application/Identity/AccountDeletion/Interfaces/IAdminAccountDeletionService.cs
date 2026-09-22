using CleanBase.Application.Identity.AccountDeletion.DTOs;
using CleanBase.Application.Common.Abstractions;

namespace CleanBase.Application.Identity.AccountDeletion.Interfaces;

public interface IAdminAccountDeletionService
{
    /// <summary>Lists accounts currently in the pending-deletion window (soonest purge first).</summary>
    Task<ErrorOr<PagedList<PendingDeletionResponse>>> ListPendingAsync(int page, int pageSize, CancellationToken ct = default);

    /// <summary>Restores a pending-deletion account, clearing the deletion flags.</summary>
    Task<ErrorOr<Success>> RestoreAsync(Guid userId, CancellationToken ct = default);
}
