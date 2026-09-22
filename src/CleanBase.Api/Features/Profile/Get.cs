using CleanBase.Application.Profile.DTOs;
using CleanBase.Application.Profile.Interfaces;

namespace CleanBase.Api.Features.Profile;

public class Get
{
    public sealed class GetProfileEndpoint(IProfileService profileService)
        : EndpointWithoutRequest<ProfileResponse>
    {
        public override void Configure()
        {
            Get("/profile");
            Description(b => b
                .WithDescription("Gets the authenticated user's profile: email, picture, voices and child profiles.")
                .WithTags("Profile"));
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var userId = User.GetUserIdAsGuid();

            var result = await profileService.GetProfileAsync(userId, ct);

            await Send.OkAsync(result, ct);
        }
    }
}
