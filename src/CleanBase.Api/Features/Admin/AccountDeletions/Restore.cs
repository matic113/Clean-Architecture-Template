using CleanBase.Application.Identity.AccountDeletion.Interfaces;
using CleanBase.Application.Common.Constants;

namespace CleanBase.Api.Features.Admin.AccountDeletions;

public class Restore
{
    public sealed record SuccessResponse(string Message);

    public sealed class RestoreEndpoint(IAdminAccountDeletionService service)
        : EndpointWithoutRequest<SuccessResponse>
    {
        public override void Configure()
        {
            Post("/admin/account-deletions/{userId}/restore");
            Roles(AppRoles.Admin);
            Description(b => b
                .WithDescription("Restores a pending-deletion account, clearing the deletion schedule (admin).")
                .WithTags("Admin"));
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var userId = Route<Guid>("userId");
            var result = await service.RestoreAsync(userId, ct);

            await Send.OkAsync(result,
                _ => new SuccessResponse("Account restored. The user can sign in again."), ct);
        }
    }
}
