using System.Security.Claims;
using CleanBase.Application.Identity.Authentication.DTOs;

namespace CleanBase.Application.Identity.Authentication.Interfaces;

public interface IJwtService
{
    JwtResult GenerateToken(IEnumerable<Claim> claims);
}