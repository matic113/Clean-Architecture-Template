using CleanBase.Application.Profile.DTOs;
using CleanBase.Application.Profile.Interfaces;

namespace CleanBase.Api.Features.Profile;

public class Update
{
    public sealed class UpdateProfileDto
    {
        public string? FirstName { get; init; }
        public string? LastName { get; init; }

        public sealed class Validator : Validator<UpdateProfileDto>
        {
            public Validator()
            {
                RuleFor(x => x.FirstName)
                    .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.")
                    .When(x => !string.IsNullOrEmpty(x.FirstName));

                RuleFor(x => x.LastName)
                    .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.")
                    .When(x => !string.IsNullOrEmpty(x.LastName));
            }
        }
    }

    public sealed class UpdateProfileEndpoint(IProfileService profileService)
        : Endpoint<UpdateProfileDto, UpdateProfileResponse>
    {
        public override void Configure()
        {
            Put("/profile");
            Description(b => b
                .WithDescription("Updates the authenticated user's profile (name).")
                .WithTags("Profile"));
        }

        public override async Task HandleAsync(UpdateProfileDto req, CancellationToken ct)
        {
            var userId = User.GetUserIdAsGuid();

            var request = new UpdateProfileRequest
            {
                FirstName = req.FirstName,
                LastName = req.LastName,
            };

            var result = await profileService.UpdateProfileAsync(userId, request, ct);

            await Send.OkAsync(result, ct);
        }
    }
}
