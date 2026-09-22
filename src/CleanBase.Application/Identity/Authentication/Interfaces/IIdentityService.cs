using CleanBase.Application.Identity.Authentication.DTOs;

namespace CleanBase.Application.Identity.Authentication.Interfaces;

public interface IIdentityService
{
    // Email/Password Authentication with OTP
    Task<ErrorOr<RegisterResponse>> RegisterWithEmailAsync(RegisterRequest request);
    Task<ErrorOr<AuthTokenResult>> VerifyEmailOtpAsync(VerifyOtpRequest request);
    Task<ErrorOr<Success>> CheckOtpAsync(VerifyOtpRequest request);
    Task<ErrorOr<AuthTokenResult>> LoginWithEmailAsync(LoginRequest request);
    Task<ErrorOr<Success>> ResendOtpAsync(ResendOtpRequest request);

    // Password Reset with OTP
    Task<ErrorOr<Success>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ErrorOr<Success>> ResetPasswordWithOtpAsync(ResetPasswordRequest request);

    // Refresh Token
    Task<ErrorOr<AuthTokenResult>> RefreshTokenAsync(string refreshToken);
}