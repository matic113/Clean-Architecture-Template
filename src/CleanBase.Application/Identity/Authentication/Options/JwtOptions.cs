namespace CleanBase.Application.Identity.Authentication.Options;

public class JwtOptions
{
    public const string JwtOptionsKey = "Authentication:JwtOptions";
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string Key { get; init; }
    public required int AccessTokenExpiryMinutes { get; init; } = 2 * 60; // 2 hours
}
