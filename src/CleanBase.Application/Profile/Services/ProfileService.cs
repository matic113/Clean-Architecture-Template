using CleanBase.Application.Profile.DTOs;
using CleanBase.Application.Profile.Interfaces;
using CleanBase.Application.Utilities.ImageProcessing;
using CleanBase.Application.Utilities.ImageProcessing.Models;
using CleanBase.Application.Utilities.Storage;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CleanBase.Application.Profile.Services;

public class ProfileService(
    UserManager<AppUser> userManager,
    IStorageService storage,
    IImageProcessor imageProcessor,
    ILogger<ProfileService> logger
) : IProfileService
{
    private const string AvatarFolder = "avatars";

    public async Task<ErrorOr<ProfileResponse>> GetProfileAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Errors.Profile.NotFound;

        return new ProfileResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePictureUrl = await ResolvePictureUrlAsync(user.ProfilePictureUrl),
        };
    }

    public async Task<ErrorOr<UpdateProfileResponse>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Errors.Profile.NotFound;

        user.FirstName = request.FirstName?.Trim();
        user.LastName = request.LastName?.Trim();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var description = string.Join(" ", result.Errors.Select(e => e.Description));
            return Errors.Profile.UpdateFailed(description);
        }

        return new UpdateProfileResponse
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
        };
    }

    public async Task<ErrorOr<ProfilePictureResponse>> UpdateProfilePictureAsync(
        Guid userId,
        Stream image,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Errors.Profile.NotFound;

        ImageResult processed;
        try
        {
            processed = await imageProcessor.ProcessAsync(image, ImagePreset.Size1000);
        }
        catch (Exception ex)
        {
            // Decoding failures come from malformed uploads — an expected failure, not a server bug.
            logger.LogWarning(ex, "Failed to process uploaded profile picture for user {UserId}", userId);
            return Errors.Image.Invalid;
        }

        var key = StorageKeys.Private(AvatarFolder, $"{Guid.NewGuid():N}.webp");

        await using (var data = new MemoryStream(processed.Data))
            await storage.UploadAsync(data, key, processed.ContentType);

        var previous = user.ProfilePictureUrl;
        user.ProfilePictureUrl = key;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var description = string.Join(" ", result.Errors.Select(e => e.Description));
            return Errors.Profile.UpdateFailed(description);
        }

        // Best-effort cleanup of the old avatar, but only if it was one we hosted — never touch an
        // external OAuth (Google/Apple) URL.
        if (IsOwnedObjectKey(previous))
            await TryDeleteObject(previous!, userId);

        return new ProfilePictureResponse
        {
            ProfilePictureUrl = await ResolvePictureUrlAsync(key),
        };
    }

    /// <summary>
    /// Turns the stored <see cref="AppUser.ProfilePictureUrl"/> into a URL the client can use:
    /// an avatar we host (object key prefixed <c>private/</c>) is signed on the fly; anything else
    /// (an external provider URL) is returned verbatim.
    /// </summary>
    private async Task<string?> ResolvePictureUrlAsync(string? stored)
    {
        if (string.IsNullOrEmpty(stored))
            return null;

        if (IsOwnedObjectKey(stored))
            return await storage.GetPresignedUrlAsync(stored);

        return stored;
    }

    private static bool IsOwnedObjectKey(string? stored) =>
        !string.IsNullOrEmpty(stored)
        && stored.StartsWith(StorageKeys.PrivatePrefix + "/", StringComparison.Ordinal);

    private async Task TryDeleteObject(string objectKey, Guid userId)
    {
        try { await storage.DeleteAsync(objectKey); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to delete avatar object {Key} for user {UserId}", objectKey, userId); }
    }
}
