using WinApps.MessageCenter.Core;

namespace WinApps.MessageCenter;

internal sealed class CliArgs
{
    public OverlayNotification? Notification { get; private set; }

    public static CliArgs Parse(string[] args)
    {
        var title = "";
        var message = "";
        var durationMs = 8000;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is "--title" && i + 1 < args.Length) title = args[++i];
            else if (a is "--message" && i + 1 < args.Length) message = args[++i];
            else if (a is "--durationMs" && i + 1 < args.Length && int.TryParse(args[++i], out var v)) durationMs = v;
        }

        var result = new CliArgs();
        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(message))
        {
            result.Notification = new OverlayNotification(title, message, durationMs);
        }
        return result;
    }
}
