using CleanBase.Application.Identity.Authentication.DTOs;

namespace CleanBase.Application.Identity.Authentication.Interfaces;

public interface IAppleAuthService
{
    Task<ErrorOr<AuthTokenResult>> HandleMobileLoginAsync(string idToken);
}
