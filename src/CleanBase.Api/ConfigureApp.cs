using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.Dashboard;
using CleanBase.Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;

namespace CleanBase.Api;

public static class ConfigureApp
{
    public static async Task ConfigureApi(this WebApplication app)
    {
        // Configure forwarded headers for the reverse proxy.
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        });

        // CORS must run before any middleware that can short-circuit the request
        app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

        // Skip in Development: the dashboard calls http://localhost:5000, and browsers
        // strip the Authorization header when following the 307 to the https origin.
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseSerilogRequestLogging();

        await app.ConfigureInfrastructure();

        app.UseHangfireDashboardWithAuth();
        app.UseRecurringJobs();

        app.MapOpenApi();
        app.MapScalarApiReference();

        app.UseExceptionHandler();

        app.UseApiEndpoints();
    }

    private static void UseHangfireDashboardWithAuth(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<HangfireDashboardOptions>>().Value;

        app.UseHangfireDashboard(
            "/hangfire",
            new DashboardOptions
            {
                Authorization =
                [
                    new BasicAuthAuthorizationFilter(options.Username, options.Password),
                ],
            }
        );
    }

    private static void UseApiEndpoints(this WebApplication app) =>
        app.UseFastEndpoints(c =>
        {
            c.Endpoints.RoutePrefix = "api";

            // Serialize/deserialize enums as their string names (e.g. "medium") instead of numbers.
            c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());

            c.Errors.ResponseBuilder = (failures, ctx, statusCode) =>
                ApiErrorResponse.FromValidationFailures(failures, ctx.Request.Path, statusCode);
        });
}
