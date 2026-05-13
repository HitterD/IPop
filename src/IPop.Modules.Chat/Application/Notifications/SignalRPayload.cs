namespace IPop.Modules.Chat.Application.Notifications;

/// <summary>
/// Legacy-compatible desktop notification payload.
/// Schema must remain stable — existing NotificationApp desktop client depends on it.
/// </summary>
public sealed record SignalRPayload(
    Guid Id,
    string ApplicationName,
    string Message,
    string Url);
