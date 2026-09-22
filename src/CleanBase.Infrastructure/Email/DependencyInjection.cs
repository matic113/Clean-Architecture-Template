using System.Net;
using System.Net.Mail;
using CleanBase.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanBase.Infrastructure.Email;

public static class DependencyInjection
{
    public static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.EmailOptionsKey));

        var emailConfig = configuration.GetSection(EmailOptions.EmailOptionsKey);
        services
            .AddFluentEmail(emailConfig["FromAddress"], emailConfig["FromName"])
            .AddRazorRenderer(AppContext.BaseDirectory)
            .AddSmtpSender(() => new SmtpClient(emailConfig["SmtpServer"]!)
            {
                Port = int.Parse(emailConfig["SmtpPort"]!),
                Credentials = new NetworkCredential(
                    emailConfig["SmtpUsername"],
                    emailConfig["SmtpPassword"]),
                EnableSsl = bool.Parse(emailConfig["EnableSsl"] ?? "true")
            });

        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
