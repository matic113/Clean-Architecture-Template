namespace CleanBase.Infrastructure.Email;

public class EmailOptions
{
    public const string EmailOptionsKey = "Email";

    public required string FromAddress { get; init; }
    public required string FromName { get; init; }
    public required string SmtpServer { get; init; }
    public required int SmtpPort { get; init; }
    public required string SmtpUsername { get; init; }
    public required string SmtpPassword { get; init; }
    public required bool EnableSsl { get; init; }
}