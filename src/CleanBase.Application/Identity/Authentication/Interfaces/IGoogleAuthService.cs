using CleanBase.Application.Identity.Authentication.DTOs;

namespace CleanBase.Application.Identity.Authentication.Interfaces;

public interface IGoogleAuthService
{
    Task<ErrorOr<AuthTokenResult>> HandleCallbackAsync();
    Task<ErrorOr<AuthTokenResult>> HandleMobileLoginAsync(string idToken);
}
