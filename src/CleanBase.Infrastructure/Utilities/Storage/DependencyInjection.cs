using CleanBase.Application.Utilities.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace CleanBase.Infrastructure.Utilities.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddObjectStorageServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var s3Options = configuration.GetSection(S3Options.SectionName).Get<S3Options>()!;
        services.AddMinio(opts =>
            opts.WithEndpoint(s3Options.Endpoint)
                .WithCredentials(s3Options.AccessKey, s3Options.SecretKey)
                .WithRegion(s3Options.Region)
                .WithSSL(s3Options.EnableSsl)
        );

        services.AddScoped<IStorageService, S3StorageService>();

        return services;
    }
}