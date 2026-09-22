using System.Linq.Expressions;
using CleanBase.Application.Identity.AccountDeletion.DTOs;
using CleanBase.Application.Identity.AccountDeletion.Interfaces;
using CleanBase.Application.Common.Abstractions;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace CleanBase.Application.Identity.AccountDeletion.Services;

public class AdminAccountDeletionService(UserManager<AppUser> userManager) : IAdminAccountDeletionService
{
    private static readonly Expression<Func<AppUser, PendingDeletionResponse>> ToResponse = u => new PendingDeletionResponse
    {
        Id = u.Id,
        Email = u.Email,
        FirstName = u.FirstName,
        LastName = u.LastName,
        DeletionRequestedAt = u.DeletionRequestedAt,
        PurgeScheduledAt = u.PurgeScheduledAt,
    };

    public async Task<ErrorOr<PagedList<PendingDeletionResponse>>> ListPendingAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = userManager.Users
            .Where(u => u.DeletionRequestedAt != null)
            .OrderBy(u => u.PurgeScheduledAt);

        return await PagedList<PendingDeletionResponse>.CreateAsync(query, ToResponse, page, pageSize);
    }

    public async Task<ErrorOr<Success>> RestoreAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Errors.Identity.UserNotFound;

        if (!user.IsPendingDeletion)
            return Errors.Identity.NotPendingDeletion;

        user.CancelDeletion();
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Errors.Identity.UserUpdateFailed;

        return Result.Success;
    }
}
