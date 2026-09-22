namespace CleanBase.Infrastructure.Utilities.Firebase;

public class FirebaseOptions
{
    public const string SectionName = "Firebase";

    /// <summary>
    /// Path to the Firebase service account JSON file, relative or absolute.
    /// </summary>
    public string? CredentialsPath { get; set; }

    /// <summary>
    /// Raw service account JSON string (useful in cloud/container environments).
    /// </summary>
    public string? CredentialsJson { get; set; }

    /// <summary>
    /// Optional Firebase project ID if not specified in the credentials file.
    /// </summary>
    public string? ProjectId { get; set; }
}
