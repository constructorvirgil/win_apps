using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using WinApps.MessageCenter.Core;

namespace WinApps.MessageCenter;

public partial class App : Application
{
    private const string MutexName = "Local\\WinApps.MessageCenter";
    private Mutex? _mutex;
    private CancellationTokenSource? _cts;
    private readonly OverlayWindowManager _windowManager = new();
    private DispatcherTimer? _idleShutdownTimer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var args = CliArgs.Parse(e.Args);

        // If another instance is already running, send via pipe and exit.
        var createdNew = false;
        _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out createdNew);
        if (!createdNew)
        {
            if (args.Notification is not null)
            {
                MessageCenterClient.TrySend(args.Notification, out _);
            }
            Shutdown(exitCode: 0);
            return;
        }

        _cts = new CancellationTokenSource();
        _ = RunPipeServerAsync(_cts.Token);

        if (args.Notification is not null)
        {
            _windowManager.Show(args.Notification);
        }

        // Exit automatically when there are no windows for a while.
        _idleShutdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _idleShutdownTimer.Tick += (_, _) =>
        {
            if (_windowManager.HasOpenWindows) return;
            Shutdown(exitCode: 0);
        };
        _idleShutdownTimer.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _cts?.Cancel(); } catch { }
        try { _cts?.Dispose(); } catch { }
        try { _mutex?.ReleaseMutex(); } catch { }
        try { _mutex?.Dispose(); } catch { }
        base.OnExit(e);
    }

    private async Task RunPipeServerAsync(CancellationToken cancellationToken)
    {
        // Loop: accept one client, read payload, show notification, repeat.
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    pipeName: MessageCenterProtocol.PipeName,
                    direction: PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    transmissionMode: PipeTransmissionMode.Byte,
                    options: PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(cancellationToken);

                using var ms = new MemoryStream();
                var buffer = new byte[4096];
                int read;
                while ((read = await server.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                var json = Encoding.UTF8.GetString(ms.ToArray());
                var notification = JsonSerializer.Deserialize<OverlayNotification>(json);
                if (notification is null) continue;

                await Dispatcher.InvokeAsync(() => _windowManager.Show(notification));
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                // Don't crash the UI host for a transient pipe error.
                await Task.Delay(200, cancellationToken);
            }
        }
    }
}
