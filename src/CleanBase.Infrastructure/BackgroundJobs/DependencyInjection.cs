using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanBase.Infrastructure.BackgroundJobs;

public static class DependencyInjection
{
    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("DefaultConnection connection string is not configured.");

        services.Configure<HangfireDashboardOptions>(configuration.GetSection(HangfireDashboardOptions.HangfireDashboard));

        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();
            config.UsePostgreSqlStorage(
                options => options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire"
                });
        });

        services.AddHangfireServer();

        return services;
    }
}
