using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace CleanBase.Application.Identity.Authentication.Interfaces;

public record RegisterUserRequest(
    string Email,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    string? ProfilePictureUrl = null,
    string? Password = null,
    UserLoginInfo? ExternalLogin = null,
    bool EmailConfirmed = false
);

public interface IUserRegistrationService
{
    Task<ErrorOr<AppUser>> RegisterAsync(RegisterUserRequest request);
}
