using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Application.Identity.Authentication.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CleanBase.Application.Identity.Authentication.Services;

public class JwtService(IOptions<JwtOptions> jwtOptions) : IJwtService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public JwtResult GenerateToken(IEnumerable<Claim> claims)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiryMinutes);

        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new JwtResult(jwtToken, expiresAt);
    }
}
