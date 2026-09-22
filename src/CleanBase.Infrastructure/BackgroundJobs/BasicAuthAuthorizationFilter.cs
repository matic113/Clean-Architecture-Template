using System.Text;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace CleanBase.Infrastructure.BackgroundJobs;

public class BasicAuthAuthorizationFilter(string username, string password) : IDashboardAuthorizationFilter
{
    private readonly string _username = username;
    private readonly string _password = password;

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var authHeader = httpContext.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader) ||
            !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        var encodedCredentials = authHeader["Basic ".Length..].Trim();

        string decodedCredentials;
        try
        {
            decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
        }
        catch (FormatException)
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        var separatorIndex = decodedCredentials.IndexOf(':');
        if (separatorIndex <= 0)
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        var username = decodedCredentials[..separatorIndex];
        var password = decodedCredentials[(separatorIndex + 1)..];

        if (username != _username || password != _password)
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        return true;
    }

    private static void SetChallengeResponse(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire Dashboard\"";
    }
}
