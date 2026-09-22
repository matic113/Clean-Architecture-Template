namespace CleanBase.Application.Utilities.Storage;

/// <summary>
/// Builds object keys with the leading visibility segment that <see cref="IStorageService"/> uses
/// to route an object to the public or private bucket. Keeping the prefix here avoids scattering the
/// "public/" / "private/" literals across the codebase.
/// </summary>
public static class StorageKeys
{
    public const string PublicPrefix = "public";
    public const string PrivatePrefix = "private";

    /// <summary>e.g. <c>Private("voice-samples", "abc.wav")</c> ⇒ <c>private/voice-samples/abc.wav</c>.</summary>
    public static string Private(string folder, string fileName) => Build(PrivatePrefix, folder, fileName);

    /// <summary>e.g. <c>Public("avatars", "abc.png")</c> ⇒ <c>public/avatars/abc.png</c>.</summary>
    public static string Public(string folder, string fileName) => Build(PublicPrefix, folder, fileName);

    private static string Build(string prefix, string folder, string fileName)
        => $"{prefix}/{folder.Trim('/')}/{fileName}";
}
