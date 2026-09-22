using CleanBase.Application.Utilities.Notifications;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace CleanBase.Infrastructure.Utilities.Firebase;

public class FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
    : IFirebaseNotificationService
{
    public async Task<ErrorOr<string>> SendToDeviceAsync(
        string deviceToken,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken ct = default
    )
    {
        try
        {
            var message = new Message
            {
                Token = deviceToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body,
                },
                Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };

            var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
            logger.LogInformation("FCM notification sent to device: {MessageId}", messageId);
            return messageId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send FCM notification to device");
            return Error.Failure("Firebase.SendFailed", ex.Message);
        }
    }

    public async Task<ErrorOr<int>> SendMulticastAsync(
        IReadOnlyList<string> deviceTokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken ct = default
    )
    {
        if (deviceTokens.Count == 0)
            return 0;

        try
        {
            var message = new MulticastMessage
            {
                Tokens = deviceTokens.ToList(),
                Notification = new Notification
                {
                    Title = title,
                    Body = body,
                },
                Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };

            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, ct);
            logger.LogInformation(
                "FCM multicast sent: {SuccessCount}/{TotalCount} succeeded",
                response.SuccessCount,
                deviceTokens.Count
            );

            return response.SuccessCount;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send multicast FCM notification");
            return Error.Failure("Firebase.MulticastFailed", ex.Message);
        }
    }

    public async Task<ErrorOr<string>> SendToTopicAsync(
        string topic,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken ct = default
    )
    {
        try
        {
            var message = new Message
            {
                Topic = topic,
                Notification = new Notification
                {
                    Title = title,
                    Body = body,
                },
                Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };

            var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
            logger.LogInformation("FCM notification sent to topic {Topic}: {MessageId}", topic, messageId);
            return messageId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send FCM notification to topic {Topic}", topic);
            return Error.Failure("Firebase.TopicSendFailed", ex.Message);
        }
    }
}
