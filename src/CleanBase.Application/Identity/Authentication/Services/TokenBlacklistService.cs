using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Application.Identity.Authentication.Options;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanBase.Application.Identity.Authentication.Services;

public class TokenBlacklistService(
    HybridCache cache,
    ILogger<TokenBlacklistService> logger,
    IOptions<JwtOptions> jwtOptions) : ITokenBlacklist
{
    private const string CacheKeyPrefix = "blacklisted_token:";
    private const string UserRevokedBeforePrefix = "user_tokens_revoked_before:";
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task RevokeTokenAsync(string jti, DateTime tokenExpiry, CancellationToken ct = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{jti}";
        var ttl = tokenExpiry - DateTime.UtcNow;

        if (ttl > TimeSpan.Zero)
        {
            await cache.SetAsync(
                cacheKey,
                true,
                new HybridCacheEntryOptions
                {
                    Expiration = ttl,
                    LocalCacheExpiration = ttl
                },
                cancellationToken: ct);
            logger.LogInformation("Token {Jti} blacklisted with TTL {Ttl}", jti, ttl);
        }
    }

    public async Task<bool> IsTokenRevokedAsync(string jti, CancellationToken ct = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{jti}";
        var result = await cache.GetOrCreateAsync(
            cacheKey,
            _ => new ValueTask<bool>(false),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(_jwtOptions.AccessTokenExpiryMinutes + 5)
            },
            cancellationToken: ct);
        return result;
    }

    public async Task SetUserTokensRevokedBeforeAsync(
        Guid userId,
        DateTime revokedBefore,
        CancellationToken ct = default)
    {
        var cacheKey = $"{UserRevokedBeforePrefix}{userId}";
        var ttl = TimeSpan.FromMinutes(_jwtOptions.AccessTokenExpiryMinutes + 5);
        await cache.SetAsync(
            cacheKey,
            revokedBefore,
            new HybridCacheEntryOptions
            {
                Expiration = ttl,
                LocalCacheExpiration = ttl
            },
            cancellationToken: ct);
    }

    public async Task<DateTime?> GetUserTokensRevokedBeforeAsync(Guid userId, CancellationToken ct = default)
    {
        var cacheKey = $"{UserRevokedBeforePrefix}{userId}";
        return await cache.GetOrCreateAsync(
            cacheKey,
            _ => new ValueTask<DateTime?>((DateTime?)null),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(_jwtOptions.AccessTokenExpiryMinutes + 5)
            },
            cancellationToken: ct);
    }
}
