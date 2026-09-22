using CleanBase.Domain.Identity;

namespace CleanBase.Application.Identity.Authentication.DTOs;

public record ResendOtpRequest
{
    public required string Email { get; init; }
    public required OtpPurpose Purpose { get; init; }
}