namespace CleanBase.Infrastructure.BackgroundJobs;

public class HangfireDashboardOptions
{
    public const string HangfireDashboard = "HangfireDashboard";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
