using CleanBase.Domain.Identity;

namespace CleanBase.Application.Identity.Authentication.Interfaces;

public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a new refresh token for the specified user
    /// </summary>
    Task<ErrorOr<RefreshToken>> GenerateRefreshTokenAsync(Guid userId);

    /// <summary>
    /// Validates a refresh token and returns the associated user ID if valid
    /// </summary>
    Task<ErrorOr<Guid>> ValidateRefreshTokenAsync(string token);

    /// <summary>
    /// Revokes a refresh token (e.g., on logout)
    /// </summary>
    Task<ErrorOr<Success>> RevokeRefreshTokenAsync(string token);

    /// <summary>
    /// Revokes all refresh tokens for a user (e.g., on password change or security event)
    /// </summary>
    Task<ErrorOr<Success>> RevokeAllUserTokensAsync(Guid userId);

    /// <summary>
    /// Rotates a refresh token: revokes the old one and creates a new one
    /// </summary>
    Task<ErrorOr<RefreshToken>> RotateRefreshTokenAsync(string oldToken);
}