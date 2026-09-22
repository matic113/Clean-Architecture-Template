using System.Security.Cryptography;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Application.Identity.Authentication.Options;
using CleanBase.Application.Common.Abstractions;
using CleanBase.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleanBase.Application.Identity.Authentication.Services;

public class RefreshTokenService(IUnitOfWork unitOfWork, IOptions<AuthOptions> authOptions) : IRefreshTokenService
{
    private readonly int RefreshTokenExpiryDays = authOptions.Value.RefreshTokenExpiryDays;

    public async Task<ErrorOr<RefreshToken>> GenerateRefreshTokenAsync(Guid userId)
    {
        var token = GenerateSecureRandomToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            Token = token,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            IsRevoked = false
        };

        var repo = unitOfWork.Repository<RefreshToken>();
        await repo.AddAsync(refreshToken);
        await unitOfWork.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<ErrorOr<Guid>> ValidateRefreshTokenAsync(string token)
    {
        var repo = unitOfWork.Repository<RefreshToken>();
        var refreshToken = await repo.GetQueryable()
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken is null)
            return Errors.Identity.InvalidRefreshToken;

        if (refreshToken.IsRevoked)
            return Errors.Identity.RevokedRefreshToken;

        if (refreshToken.IsExpired)
            return Errors.Identity.ExpiredRefreshToken;

        return refreshToken.UserId;
    }

    public async Task<ErrorOr<Success>> RevokeRefreshTokenAsync(string token)
    {
        var repo = unitOfWork.Repository<RefreshToken>();
        var refreshToken = await repo.GetQueryable()
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken is null)
            return Errors.Identity.InvalidRefreshToken;

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;

        repo.Update(refreshToken);
        await unitOfWork.SaveChangesAsync();

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> RevokeAllUserTokensAsync(Guid userId)
    {
        var repo = unitOfWork.Repository<RefreshToken>();
        var userTokens = await repo.GetQueryable()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in userTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            repo.Update(token);
        }

        await unitOfWork.SaveChangesAsync();

        return Result.Success;
    }

    public async Task<ErrorOr<RefreshToken>> RotateRefreshTokenAsync(string oldToken)
    {
        var validationResult = await ValidateRefreshTokenAsync(oldToken);
        if (validationResult.IsError)
            return validationResult.Errors;

        var userId = validationResult.Value;

        var repo = unitOfWork.Repository<RefreshToken>();
        var oldRefreshToken = await repo.GetQueryable()
            .FirstOrDefaultAsync(rt => rt.Token == oldToken);

        if (oldRefreshToken is null)
            return Errors.Identity.InvalidRefreshToken;

        var newTokenResult = await GenerateRefreshTokenAsync(userId);
        if (newTokenResult.IsError)
            return newTokenResult.Errors;

        oldRefreshToken.IsRevoked = true;
        oldRefreshToken.RevokedAt = DateTime.UtcNow;
        oldRefreshToken.ReplacedByTokenId = newTokenResult.Value.Id;

        repo.Update(oldRefreshToken);
        await unitOfWork.SaveChangesAsync();

        return newTokenResult.Value;
    }

    private static string GenerateSecureRandomToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
