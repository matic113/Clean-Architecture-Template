namespace CleanBase.Application.Identity.Authentication.DTOs;

public record RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
}
