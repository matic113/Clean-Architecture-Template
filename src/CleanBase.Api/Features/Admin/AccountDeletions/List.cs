using CleanBase.Application.Identity.AccountDeletion.DTOs;
using CleanBase.Application.Identity.AccountDeletion.Interfaces;
using CleanBase.Application.Common.Abstractions;
using CleanBase.Application.Common.Constants;

namespace CleanBase.Api.Features.Admin.AccountDeletions;

public class List
{
    public sealed record ListDto
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public sealed class ListEndpoint(IAdminAccountDeletionService service)
        : Endpoint<ListDto, PagedList<PendingDeletionResponse>>
    {
        public override void Configure()
        {
            Get("/admin/account-deletions");
            Roles(AppRoles.Admin);
            Description(b => b
                .WithDescription("Lists accounts currently in the pending-deletion window (admin).")
                .WithTags("Admin"));
        }

        public override async Task HandleAsync(ListDto req, CancellationToken ct)
        {
            var page = req.Page < 1 ? 1 : req.Page;
            var pageSize = req.PageSize is < 1 or > 100 ? 20 : req.PageSize;

            var result = await service.ListPendingAsync(page, pageSize, ct);

            await Send.OkAsync(result, ct);
        }
    }
}
