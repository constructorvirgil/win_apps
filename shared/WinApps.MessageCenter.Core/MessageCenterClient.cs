using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace WinApps.MessageCenter.Core;

public static class MessageCenterClient
{
    public static bool TrySend(OverlayNotification notification, out string? error)
    {
        return TrySend(MessageCenterProtocol.PipeName, notification, out error);
    }

    public static bool TrySend(string pipeName, OverlayNotification notification, out string? error)
    {
        error = null;

        try
        {
            if (notification is null) throw new ArgumentNullException(nameof(notification));
            if (string.IsNullOrWhiteSpace(notification.Title)) throw new ArgumentException("Title is required.", nameof(notification));
            if (string.IsNullOrWhiteSpace(notification.Message)) throw new ArgumentException("Message is required.", nameof(notification));

            var payload = JsonSerializer.Serialize(notification);
            var bytes = Encoding.UTF8.GetBytes(payload);

            using var client = new NamedPipeClientStream(
                serverName: ".",
                pipeName: pipeName,
                direction: PipeDirection.Out,
                options: PipeOptions.Asynchronous);

            // Keep this low; sender should be fast and non-blocking.
            client.Connect(timeout: 250);
            client.Write(bytes, 0, bytes.Length);
            client.Flush();

            return true;
        }
        catch (TimeoutException)
        {
            error = "Timeout connecting to message center pipe.";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
