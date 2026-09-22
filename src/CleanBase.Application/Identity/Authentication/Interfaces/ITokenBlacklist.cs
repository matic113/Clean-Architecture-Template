namespace CleanBase.Application.Identity.Authentication.Interfaces;

public interface ITokenBlacklist
{
    Task RevokeTokenAsync(string jti, DateTime tokenExpiry, CancellationToken ct = default);
    Task<bool> IsTokenRevokedAsync(string jti, CancellationToken ct = default);
    Task SetUserTokensRevokedBeforeAsync(Guid userId, DateTime revokedBefore, CancellationToken ct = default);
    Task<DateTime?> GetUserTokensRevokedBeforeAsync(Guid userId, CancellationToken ct = default);
}
