namespace CleanBase.Application.Identity.Authentication.Options;

public class AppleAuthOptions
{
    public const string SectionName = "Authentication:Apple";

    public string[] BundleIds { get; set; } = [];
}
