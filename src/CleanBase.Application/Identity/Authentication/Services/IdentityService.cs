using Hangfire;
using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Application.Common.Interfaces;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CleanBase.Application.Identity.Authentication.Services;

public class IdentityService(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IJwtService jwtService,
    IClaimsProvider claimsProvider,
    IOtpService otpService,
    IRefreshTokenService refreshTokenService,
    IUserRegistrationService userRegistrationService,
    IBackgroundJobClient backgroundJobClient,
    ILogger<IdentityService> logger
)
    : IIdentityService
{
    public async Task<ErrorOr<RegisterResponse>> RegisterWithEmailAsync(RegisterRequest request)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return existingUser.IsPendingDeletion
                ? Errors.Identity.AccountPendingDeletion
                : Errors.Identity.EmailAlreadyExists;

        var registrationResult = await userRegistrationService.RegisterAsync(new RegisterUserRequest(
            Email: request.Email,
            FirstName: request.FirstName,
            LastName: request.LastName,
            PhoneNumber: request.PhoneNumber,
            Password: request.Password,
            EmailConfirmed: false));

        if (registrationResult.IsError)
        {
            return Errors.Identity.RegistrationFailed($"Failed to register user: {string.Join(", ", registrationResult.Errors.Select(e => e.Description))}");
        }

        var user = registrationResult.Value;

        var otpResult = await otpService.GenerateOtpAsync(request.Email, OtpPurpose.EmailVerification);
        if (otpResult.IsError)
        {
            await userManager.DeleteAsync(user);
            return otpResult.Errors;
        }

        backgroundJobClient.Enqueue<IEmailService>(svc =>
            svc.SendOtpEmailAsync(request.Email, otpResult.Value, OtpPurpose.EmailVerification));

        return new RegisterResponse
        {
            Email = request.Email,
            Message = "Registration successful! Please check your email for the verification code."
        };
    }

    public async Task<ErrorOr<AuthTokenResult>> VerifyEmailOtpAsync(VerifyOtpRequest request)
    {
        var otpResult = await otpService.ValidateOtpAsync(request.Email, request.Code, request.Purpose);
        if (otpResult.IsError)
            return otpResult.Errors;

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Errors.Identity.UserNotFound;

        if (user.IsPendingDeletion)
            return Errors.Identity.AccountPendingDeletion;

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            var updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
                return Errors.Identity.UserUpdateFailed;
        }

        logger.LogInformation("User {UserId} successfully verified email {Email}", user.Id, request.Email);

        var claims = await claimsProvider.GetClaimsAsync(user.Id);
        var jwtResult = jwtService.GenerateToken(claims);

        var refreshTokenResult = await refreshTokenService.GenerateRefreshTokenAsync(user.Id);
        if (refreshTokenResult.IsError)
            return refreshTokenResult.Errors;

        return new AuthTokenResult(jwtResult.Token, refreshTokenResult.Value.Token, jwtResult.ExpiresAt);
    }

    public async Task<ErrorOr<Success>> CheckOtpAsync(VerifyOtpRequest request)
    {
        var otpResult =
            await otpService.ValidateOtpAsync(request.Email, request.Code, request.Purpose, consume: false);
        return otpResult.IsError ? otpResult.Errors : Result.Success;
    }

    public async Task<ErrorOr<AuthTokenResult>> LoginWithEmailAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || user.UserName != request.Email)
            return Errors.Identity.InvalidCredentials;

        if (user.IsPendingDeletion)
            return Errors.Identity.AccountPendingDeletion;

        var signInResult = await signInManager.PasswordSignInAsync(
            user.UserName,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
            return Errors.Identity.InvalidCredentials;

        if (!user.EmailConfirmed)
            return Errors.Identity.EmailNotConfirmed;

        var claims = await claimsProvider.GetClaimsAsync(user.Id);
        var jwtResult = jwtService.GenerateToken(claims);

        var refreshTokenResult = await refreshTokenService.GenerateRefreshTokenAsync(user.Id);
        if (refreshTokenResult.IsError)
            return refreshTokenResult.Errors;

        return new AuthTokenResult(jwtResult.Token, refreshTokenResult.Value.Token, jwtResult.ExpiresAt);
    }

    public async Task<ErrorOr<Success>> ResendOtpAsync(ResendOtpRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Errors.Identity.UserNotFound;

        var emailVerified = user.EmailConfirmed;
        if (request.Purpose == OtpPurpose.EmailVerification && emailVerified)
            return Errors.Identity.EmailAlreadyVerified;

        var canResend = await otpService.CanResendOtpAsync(request.Email, request.Purpose);
        if (canResend.IsError)
            return canResend.Errors;

        var otpResult = await otpService.GenerateOtpAsync(request.Email, request.Purpose);
        if (otpResult.IsError)
            return otpResult.Errors;

        backgroundJobClient.Enqueue<IEmailService>(svc =>
            svc.SendOtpEmailAsync(request.Email, otpResult.Value, request.Purpose));

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Errors.Identity.UserNotFound;

        var canResend = await otpService.CanResendOtpAsync(request.Email, OtpPurpose.PasswordReset);
        if (canResend.IsError)
            return canResend.Errors;

        var otpResult = await otpService.GenerateOtpAsync(request.Email, OtpPurpose.PasswordReset);
        if (otpResult.IsError)
            return otpResult.Errors;

        backgroundJobClient.Enqueue<IEmailService>(svc =>
            svc.SendOtpEmailAsync(request.Email, otpResult.Value, OtpPurpose.PasswordReset));

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ResetPasswordWithOtpAsync(ResetPasswordRequest request)
    {
        var otpResult = await otpService.ValidateOtpAsync(request.Email, request.Code, OtpPurpose.PasswordReset);
        if (otpResult.IsError)
            return otpResult.Errors;

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Errors.Identity.UserNotFound;

        await userManager.RemovePasswordAsync(user);
        var resetResult = await userManager.AddPasswordAsync(user, request.NewPassword);

        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
            return Errors.Identity.PasswordValidationFailed(errors);
        }

        return Result.Success;
    }

    public async Task<ErrorOr<AuthTokenResult>> RefreshTokenAsync(string refreshToken)
    {
        var newRefreshTokenResult = await refreshTokenService.RotateRefreshTokenAsync(refreshToken);
        if (newRefreshTokenResult.IsError)
            return newRefreshTokenResult.Errors;

        var newRefreshToken = newRefreshTokenResult.Value;

        var user = await userManager.FindByIdAsync(newRefreshToken.UserId.ToString());
        if (user is null)
            return Errors.Identity.UserNotFound;

        if (user.IsPendingDeletion)
            return Errors.Identity.AccountPendingDeletion;

        var claims = await claimsProvider.GetClaimsAsync(user.Id);
        var jwtResult = jwtService.GenerateToken(claims);

        return new AuthTokenResult(jwtResult.Token, newRefreshToken.Token, jwtResult.ExpiresAt);
    }
}
