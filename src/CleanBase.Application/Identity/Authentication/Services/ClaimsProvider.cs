using System.Security.Claims;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Application.Common.Constants;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CleanBase.Application.Identity.Authentication.Services;

public class ClaimsProvider(UserManager<AppUser> userManager) : IClaimsProvider
{
    public async Task<List<Claim>> GetClaimsAsync(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString())
        };

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return claims;

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

        if (!string.IsNullOrEmpty(user.FirstName) || !string.IsNullOrEmpty(user.LastName))
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                claims.Add(new Claim(JwtRegisteredClaimNames.Name, fullName));
        }

        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            claims.Add(new Claim("picture", user.ProfilePictureUrl));

        var hasPassword = await userManager.HasPasswordAsync(user);
        claims.Add(new Claim(AppClaims.PasswordSet, hasPassword ? "true" : "false"));

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }
}
