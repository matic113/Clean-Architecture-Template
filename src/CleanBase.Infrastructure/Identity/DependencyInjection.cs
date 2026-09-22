using System.Text;
using CleanBase.Application.Identity.Authentication.Interfaces;
using CleanBase.Application.Identity.Authentication.Options;
using CleanBase.Domain.Identity;
using CleanBase.Infrastructure.Persistence;
using CleanBase.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CleanBase.Infrastructure.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddIdentity<AppUser, AppRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 0;

                options.SignIn.RequireConfirmedEmail = false;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddGoogle(options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"]
                ?? throw new ArgumentException("Authentication:Google:ClientId environment variable is not set.");

                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]
                ?? throw new ArgumentException("Authentication:Google:ClientSecret environment variable is not set.");

                options.ClaimActions.MapJsonKey("picture", "picture");
                options.ClaimActions.MapJsonKey("given_name", "given_name");
                options.ClaimActions.MapJsonKey("family_name", "family_name");
            })
            .AddJwtBearer(options =>
            {
                var jwtOptions =
                    configuration.GetSection(JwtOptions.JwtOptionsKey).Get<JwtOptions>()
                    ?? throw new ArgumentException(nameof(JwtOptions));

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirst("jti")?.Value;
                        if (string.IsNullOrEmpty(jti)) return;

                        var blacklist = context.HttpContext.RequestServices
                            .GetRequiredService<ITokenBlacklist>();

                        if (await blacklist.IsTokenRevokedAsync(jti))
                            context.Fail("Token has been revoked.");

                        var userIdValue = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                        if (string.IsNullOrEmpty(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                            return;

                        var revokedBefore = await blacklist.GetUserTokensRevokedBeforeAsync(userId);
                        if (!revokedBefore.HasValue)
                            return;

                        var iatValue = context.Principal?.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
                        if (string.IsNullOrEmpty(iatValue) || !long.TryParse(iatValue, out var iatSeconds))
                            return;

                        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatSeconds).UtcDateTime;
                        if (issuedAt < revokedBefore.Value)
                            context.Fail("Token has been revoked.");
                    }
                };
            });

        services.AddAuthorization(options =>
        {
        });

        services.AddHttpClient("AppleJwks");

        services.AddHttpContextAccessor();

        services.AddScoped<IdentitySeeder>();

        return services;
    }
}
