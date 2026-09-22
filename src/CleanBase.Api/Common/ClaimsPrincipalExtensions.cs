using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace CleanBase.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new InvalidOperationException("User ID claim not found");

    public static string GetEmail(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Email)
           ?? user.FindFirstValue("email")
           ?? throw new InvalidOperationException("Email claim not found");

    public static string? GetJwtId(this ClaimsPrincipal principal)
        => principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

    public static DateTime? GetTokenExpiry(this ClaimsPrincipal principal)
    {
        var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

        if (string.IsNullOrEmpty(expClaim) || !long.TryParse(expClaim, out long expUnixSeconds))
            return null;

        return DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds).UtcDateTime;
    }

    public static Guid GetUserIdAsGuid(this ClaimsPrincipal principal)
    {
        var userIdString = principal.GetUserId();

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            throw new InvalidOperationException("User ID is not a valid Guid.");
        }

        return userId;
    }
}