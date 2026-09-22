namespace CleanBase.Application.Profile.DTOs;
 
public record ProfileResponse
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string? FirstName { get; init; }
    public string? LastName { get; init; }

    /// <summary>
    /// Ready-to-use picture URL: a presigned URL for an avatar we host, or the external
    /// provider URL (Google/Apple) verbatim. Null when the user has no picture.
    /// </summary>
    public string? ProfilePictureUrl { get; init; }
}
