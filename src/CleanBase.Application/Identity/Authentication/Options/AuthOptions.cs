namespace CleanBase.Application.Identity.Authentication.Options;

public class AuthOptions
{
    public const string AuthenticationOptionsKey = "Authentication:Options";
    public required int RefreshTokenExpiryDays { get; init; } = 30;
    public required string[] AllowedRedirectUris { get; init; }

    public bool IsRedirectUriAllowed(string redirectUri)
    {
        return AllowedRedirectUris.Any(allowed =>
            redirectUri.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) || allowed == "*");
    }
}
