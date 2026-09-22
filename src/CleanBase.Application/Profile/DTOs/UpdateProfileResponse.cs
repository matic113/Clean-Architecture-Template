namespace CleanBase.Application.Profile.DTOs;

public record UpdateProfileResponse
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}
