namespace CleanBase.Application.Identity.Authentication.DTOs;

public record RegisterResponse
{
    public required string Email { get; init; }
    public required string Message { get; init; }
}