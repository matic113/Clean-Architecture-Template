using CleanBase.Application.Profile.DTOs;
using CleanBase.Application.Profile.Interfaces;

namespace CleanBase.Api.Features.Profile;

public class UpdatePicture
{
    public sealed class UpdateProfilePictureDto
    {
        public IFormFile Picture { get; init; } = default!;

        public sealed class Validator : Validator<UpdateProfilePictureDto>
        {
            public Validator()
            {
                RuleFor(x => x.Picture)
                    .NotNull().WithMessage("A picture image is required.");

                RuleFor(x => x.Picture.ContentType)
                    .Must(ct => ct.StartsWith("image/"))
                    .When(x => x.Picture is not null)
                    .WithMessage("The profile picture must be an image file.");

                RuleFor(x => x.Picture.Length)
                    .LessThanOrEqualTo(10 * 1024 * 1024)
                    .When(x => x.Picture is not null)
                    .WithMessage("The profile picture cannot exceed 10 MB.");
            }
        }
    }

    public sealed class UpdateProfilePictureEndpoint(IProfileService profileService)
        : Endpoint<UpdateProfilePictureDto, ProfilePictureResponse>
    {
        public override void Configure()
        {
            Put("/profile/picture");
            AllowFileUploads();
            Description(b => b
                .WithDescription("Uploads or replaces the authenticated user's profile picture.")
                .WithTags("Profile"));
        }

        public override async Task HandleAsync(UpdateProfilePictureDto req, CancellationToken ct)
        {
            var userId = User.GetUserIdAsGuid();

            await using var picture = req.Picture.OpenReadStream();

            var result = await profileService.UpdateProfilePictureAsync(userId, picture, ct);

            await Send.OkAsync(result, ct);
        }
    }
}
