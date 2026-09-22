namespace CleanBase.Application.Identity.Authentication.DTOs;

public record ResetPasswordRequest
{
    public required string Email { get; init; }
    public required string Code { get; init; }
    public required string NewPassword { get; init; }
}