using Hangfire;
using CleanBase.Application.Identity.AccountDeletion.DTOs;
using CleanBase.Application.Identity.AccountDeletion.Interfaces;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Application.Common.Interfaces;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CleanBase.Application.Identity.AccountDeletion.Services;

public class AccountDeletionService(
    UserManager<AppUser> userManager,
    IOtpService otpService,
    IRefreshTokenService refreshTokenService,
    ITokenBlacklist tokenBlacklist,
    IBackgroundJobClient backgroundJobClient,
    ILogger<AccountDeletionService> logger
) : IAccountDeletionService
{
    /// <summary>Reversible retention window before an account is permanently purged.</summary>
    public const int RetentionDays = 30;

    public async Task<ErrorOr<Success>> RequestDeletionAsync(RequestAccountDeletionRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        // Non-enumerating: never reveal whether the email maps to an account. Already-pending
        // accounts don't need another OTP — treat both as a no-op success.
        if (user is null || user.IsPendingDeletion)
            return Result.Success;

        var canResend = await otpService.CanResendOtpAsync(request.Email, OtpPurpose.AccountDeletion);
        if (canResend.IsError)
            return canResend.Errors;

        var otpResult = await otpService.GenerateOtpAsync(request.Email, OtpPurpose.AccountDeletion);
        if (otpResult.IsError)
            return otpResult.Errors;

        backgroundJobClient.Enqueue<IEmailService>(svc =>
            svc.SendOtpEmailAsync(request.Email, otpResult.Value, OtpPurpose.AccountDeletion));

        return Result.Success;
    }

    public async Task<ErrorOr<AccountDeletionScheduledResult>> ConfirmDeletionAsync(ConfirmAccountDeletionRequest request)
    {
        var otpResult = await otpService.ValidateOtpAsync(request.Email, request.Code, OtpPurpose.AccountDeletion);
        if (otpResult.IsError)
            return otpResult.Errors;

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Errors.Identity.UserNotFound;

        // Idempotent: a second confirmation within the window just returns the existing schedule.
        if (user.IsPendingDeletion)
            return new AccountDeletionScheduledResult(user.PurgeScheduledAt!.Value);

        user.RequestDeletion(RetentionDays);
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Errors.Identity.UserUpdateFailed;

        // Revoke all refresh tokens for the user to prevent further access with existing tokens.
        await refreshTokenService.RevokeAllUserTokensAsync(user.Id);

        // Revoke current jwt tokens
        await tokenBlacklist.SetUserTokensRevokedBeforeAsync(user.Id, DateTime.UtcNow);

        logger.LogInformation(
            "User {UserId} confirmed account deletion; scheduled for purge at {PurgeAt}",
            user.Id, user.PurgeScheduledAt);

        return new AccountDeletionScheduledResult(user.PurgeScheduledAt!.Value);
    }
}
