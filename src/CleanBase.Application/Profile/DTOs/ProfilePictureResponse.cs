namespace CleanBase.Application.Profile.DTOs;

public record ProfilePictureResponse
{
    /// <summary>Ready-to-use presigned URL for the newly uploaded avatar.</summary>
    public string? ProfilePictureUrl { get; init; }
}
