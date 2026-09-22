using CleanBase.Application.Utilities.ImageProcessing;
using Microsoft.Extensions.DependencyInjection;

namespace CleanBase.Infrastructure.Utilities.ImageProcessing;

public static class DependencyInjection
{
    public static IServiceCollection AddImageProcessing(this IServiceCollection services)
    {
        services.AddSingleton<IImageProcessor, ImageProcessor>();
        return services;
    }
}