namespace CleanBase.Application.Identity.Authentication.DTOs;

public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}