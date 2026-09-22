using CleanBase.Application.Identity;
using CleanBase.Application.Profile;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanBase.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIdentityServices(configuration);
        services.AddProfileServices();

        return services;
    }
}
