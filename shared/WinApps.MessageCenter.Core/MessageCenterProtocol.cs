namespace WinApps.MessageCenter.Core;

public static class MessageCenterProtocol
{
    public const string PipeName = "WinApps.MessageCenter";
}

public sealed record OverlayNotification(
    string Title,
    string Message,
    int DurationMs = 8000);
