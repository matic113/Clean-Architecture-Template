using CleanBase.Application.Common.Abstractions;
using CleanBase.Infrastructure.BackgroundJobs;
using CleanBase.Infrastructure.Email;
using CleanBase.Infrastructure.Identity;
using CleanBase.Infrastructure.Persistence;
using CleanBase.Infrastructure.Persistence.Repositories;
using CleanBase.Infrastructure.Utilities.AI.OpenRouter;
using CleanBase.Infrastructure.Utilities.Firebase;
using CleanBase.Infrastructure.Utilities.ImageProcessing;
using CleanBase.Infrastructure.Utilities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanBase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDatabase(configuration);
        services.AddRedis(configuration);
        services.AddHybridCache();

        services.AddObjectStorageServices(configuration);
        services.AddImageProcessing();
        services.AddOpenRouterServices(configuration);
        services.AddFirebaseServices(configuration);

        services.AddIdentityServices(configuration);
        services.AddEmailServices(configuration);
        services.AddHangfireServices(configuration);

        // Register services
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("DefaultConnection connection string is not configured.");

        services.AddDbContext<ApplicationDbContext>(opt =>
        {
            opt.UseNpgsql(connectionString);
            opt.UseSnakeCaseNamingConvention();
        });
    }

    private static void AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            var redisConnection = configuration.GetConnectionString("RedisConnectionString");
            if (string.IsNullOrEmpty(redisConnection))
                throw new ArgumentException("Redis connection string is not configured.");

            options.Configuration = redisConnection;
        });
    }

}
