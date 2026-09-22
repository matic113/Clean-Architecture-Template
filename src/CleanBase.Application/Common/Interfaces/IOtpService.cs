using CleanBase.Domain.Identity;

namespace CleanBase.Application.Common.Interfaces;

public sealed record OtpValidationResult(bool Valid);

public interface IOtpService
{
    /// <summary>
    /// Generates and stores a new OTP for the given email and purpose.
    /// </summary>
    Task<ErrorOr<string>> GenerateOtpAsync(string email, OtpPurpose purpose);

    /// <summary>
    /// Validates an OTP code for a given email and purpose.
    /// </summary>
    Task<ErrorOr<OtpValidationResult>> ValidateOtpAsync(string email, string code, OtpPurpose purpose, bool consume = true);

    /// <summary>
    /// Checks if an OTP can be resent (cooldown period check)
    /// </summary>
    Task<ErrorOr<Success>> CanResendOtpAsync(string email, OtpPurpose purpose);
}
