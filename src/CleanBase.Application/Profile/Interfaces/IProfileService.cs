using CleanBase.Application.Profile.DTOs;

namespace CleanBase.Application.Profile.Interfaces;

public interface IProfileService
{
    Task<ErrorOr<ProfileResponse>> GetProfileAsync(Guid userId, CancellationToken ct = default);

    Task<ErrorOr<UpdateProfileResponse>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken ct = default
    );

    Task<ErrorOr<ProfilePictureResponse>> UpdateProfilePictureAsync(
        Guid userId,
        Stream image,
        CancellationToken ct = default
    );
}
