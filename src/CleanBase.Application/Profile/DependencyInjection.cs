using CleanBase.Application.Profile.Interfaces;
using CleanBase.Application.Profile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CleanBase.Application.Profile;

public static class DependencyInjection
{
    public static IServiceCollection AddProfileServices(this IServiceCollection services)
    {
        services.AddScoped<IProfileService, ProfileService>();

        return services;
    }
}
