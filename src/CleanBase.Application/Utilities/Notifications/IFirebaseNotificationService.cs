namespace CleanBase.Application.Utilities.Notifications;

public interface IFirebaseNotificationService
{
    /// <summary>
    /// Sends a push notification to a single device token.
    /// </summary>
    Task<ErrorOr<string>> SendToDeviceAsync(
        string deviceToken,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Sends a push notification to multiple device tokens.
    /// </summary>
    Task<ErrorOr<int>> SendMulticastAsync(
        IReadOnlyList<string> deviceTokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Sends a push notification to all devices subscribed to a topic.
    /// </summary>
    Task<ErrorOr<string>> SendToTopicAsync(
        string topic,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken ct = default
    );
}
