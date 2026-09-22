namespace CleanBase.Application.Identity.Authentication.DTOs;

public record ForgotPasswordRequest
{
    public required string Email { get; init; }
}