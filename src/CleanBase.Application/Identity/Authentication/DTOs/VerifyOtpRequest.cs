using CleanBase.Domain.Identity;

namespace CleanBase.Application.Identity.Authentication.DTOs;

public record VerifyOtpRequest
{
    public required string Email { get; init; }
    public required string Code { get; init; }
    public required OtpPurpose Purpose { get; init; }
}