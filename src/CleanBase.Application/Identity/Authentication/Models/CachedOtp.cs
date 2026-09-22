using CleanBase.Domain.Identity;

namespace CleanBase.Application.Identity.Authentication.Models;

public sealed record CachedOtp(
    string Code,
    OtpPurpose Purpose,
    DateTime CreatedAt
);
