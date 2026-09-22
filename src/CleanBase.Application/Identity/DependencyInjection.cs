using CleanBase.Application.Identity.AccountDeletion.Interfaces;
using CleanBase.Application.Identity.AccountDeletion.Services;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Application.Identity.Authentication.Options;
using CleanBase.Application.Identity.Authentication.Services;
using CleanBase.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanBase.Application.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthenticationServices(configuration);
        services.AddAccountDeletionServices();

        return services;
    }

    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.JwtOptionsKey));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.AuthenticationOptionsKey));
        services.Configure<AppleAuthOptions>(configuration.GetSection(AppleAuthOptions.SectionName));

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IAppleAuthService, AppleAuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IClaimsProvider, ClaimsProvider>();
        services.AddScoped<ITokenBlacklist, TokenBlacklistService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        return services;
    }

    public static IServiceCollection AddAccountDeletionServices(this IServiceCollection services)
    {
        services.AddScoped<IAccountDeletionService, AccountDeletionService>();
        services.AddScoped<IAdminAccountDeletionService, AdminAccountDeletionService>();
        services.AddScoped<IUserPurgeService, UserPurgeService>();

        return services;
    }
}
