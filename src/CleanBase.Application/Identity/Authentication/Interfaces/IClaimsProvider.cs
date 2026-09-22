using System.Security.Claims;

namespace CleanBase.Application.Identity.Authentication.Interfaces;

public interface IClaimsProvider
{
    Task<List<Claim>> GetClaimsAsync(Guid userId);
}