namespace CleanBase.Application.Profile.DTOs;

public record UpdateProfileRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}
