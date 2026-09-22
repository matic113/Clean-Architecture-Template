using CleanBase.Application.Utilities.Notifications;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CleanBase.Infrastructure.Utilities.Firebase;

public static class DependencyInjection
{
    public static IServiceCollection AddFirebaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName));

        var options = configuration.GetSection(FirebaseOptions.SectionName).Get<FirebaseOptions>();

        // Only initialize FirebaseApp if credentials are provided.
        // If not configured, FirebaseApp.DefaultInstance remains null and any calls gracefully return an error.
        try
        {
            GoogleCredential? credential = null;

#pragma warning disable CS0618 // GoogleCredential factory methods are undergoing API migration in Google.Apis.Auth
            if (!string.IsNullOrWhiteSpace(options?.CredentialsJson))
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(options.CredentialsJson));
                credential = GoogleCredential.FromStream(stream);
            }
            else if (!string.IsNullOrWhiteSpace(options?.CredentialsPath) && File.Exists(options.CredentialsPath))
            {
                using var stream = File.OpenRead(options.CredentialsPath);
                credential = GoogleCredential.FromStream(stream);
            }
#pragma warning restore CS0618

            if (credential is not null && FirebaseApp.DefaultInstance is null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = options?.ProjectId,
                });
            }
        }
        catch (Exception ex)
        {
            var logger = services.BuildServiceProvider().GetService<ILogger<FirebaseOptions>>();
            logger?.LogWarning(ex, "Failed to initialize FirebaseApp. Push notifications will be disabled until configured.");
        }

        services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();

        return services;
    }
}
