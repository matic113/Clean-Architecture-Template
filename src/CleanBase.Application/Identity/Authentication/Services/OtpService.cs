using System.Security.Cryptography;
using CleanBase.Application.Identity.Authentication.Models;
using CleanBase.Application.Common.Interfaces;
using CleanBase.Domain.Identity;
using Microsoft.Extensions.Caching.Hybrid;

namespace CleanBase.Application.Identity.Authentication.Services;

public class OtpService(HybridCache cache) : IOtpService
{
    private const int OtpLength = 6;
    private const int OtpExpiryMinutes = 10;
    private const int ResendCooldownSeconds = 60;

    private string GetCacheKey(string email, OtpPurpose purpose)
        => $"otp:{email.ToLowerInvariant()}:{purpose}";

    public async Task<ErrorOr<string>> GenerateOtpAsync(string email, OtpPurpose purpose)
    {
        var code = GenerateNumericCode(OtpLength);

        var cachedOtp = new CachedOtp(
            Code: code,
            Purpose: purpose,
            CreatedAt: DateTime.UtcNow
        );

        var key = GetCacheKey(email, purpose);

        await cache.SetAsync(
            key,
            cachedOtp,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(OtpExpiryMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(OtpExpiryMinutes)
            },
            tags: [$"user:{email.ToLowerInvariant()}", "otp"]
        );

        return code;
    }

    public async Task<ErrorOr<OtpValidationResult>> ValidateOtpAsync(string email, string code, OtpPurpose purpose,
        bool consume = true)
    {
        var key = GetCacheKey(email, purpose);
        var cachedOtp = await cache.GetOrCreateAsync(
            key,
            async _ => (CachedOtp?)null,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(OtpExpiryMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(OtpExpiryMinutes)
            }
        );

        if (cachedOtp == null)
            return Errors.Identity.InvalidOtp;

        if (cachedOtp.Purpose != purpose)
            return Errors.Identity.InvalidOtp;

        if (cachedOtp.Code != code)
            return Errors.Identity.InvalidOtp;

        if (consume)
        {
            await cache.RemoveAsync(key);
        }

        return new OtpValidationResult(true);
    }

    public async Task<ErrorOr<Success>> CanResendOtpAsync(string email, OtpPurpose purpose)
    {
        var key = GetCacheKey(email, purpose);
        var cachedOtp = await cache.GetOrCreateAsync(
            key,
            async _ => (CachedOtp?)null,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(OtpExpiryMinutes)
            }
        );

        if (cachedOtp != null)
        {
            var timeSinceCreation = DateTime.UtcNow - cachedOtp.CreatedAt;
            if (timeSinceCreation.TotalSeconds < ResendCooldownSeconds)
            {
                var remainingSeconds = ResendCooldownSeconds - (int)timeSinceCreation.TotalSeconds;
                return Errors.Identity.OtpResendCooldown(remainingSeconds);
            }
        }

        return Result.Success;
    }

    private static string GenerateNumericCode(int length)
    {
        var code = new char[length];
        for (int i = 0; i < length; i++)
        {
            code[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(code);
    }
}
