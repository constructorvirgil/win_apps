using System.IO.Pipes;
using System.Text.Json;

namespace drvercheck.Probe;

internal static class ProbeHost
{
    public static int Run(string[] args)
    {
        string pipeName = GetOption(args, "--pipe") ?? "drvercheck";

        using var appReady = new ManualResetEventSlim(false);
        using var shutdown = new CancellationTokenSource();

        ProbeWindow? window = null;

        var uiThread = new Thread(() =>
        {
            ApplicationConfiguration.Initialize();
            window = new ProbeWindow();
            window.Shown += (_, _) =>
            {
                window.EnsureForegroundAndFocus();
                appReady.Set();
            };
            Application.Run(window);
            shutdown.Cancel();
        })
        {
            IsBackground = true
        };
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();

        if (!appReady.Wait(TimeSpan.FromSeconds(10)))
        {
            Console.Error.WriteLine("Probe UI failed to start.");
            return 1;
        }

        if (window == null)
        {
            Console.Error.WriteLine("Probe window not available.");
            return 1;
        }

        try
        {
            RunPipeServer(pipeName, window, shutdown.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            try
            {
                if (window.IsHandleCreated)
                    window.BeginInvoke(() => window.Close());
            }
            catch { }
        }
    }

    private static void RunPipeServer(string pipeName, ProbeWindow window, CancellationToken token)
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        using var server = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous);

        server.WaitForConnection();

        using var reader = new StreamReader(server);
        using var writer = new StreamWriter(server) { AutoFlush = true };

        while (!token.IsCancellationRequested)
        {
            var line = reader.ReadLine();
            if (line == null) break;

            ProbeRequest? req;
            try
            {
                req = JsonSerializer.Deserialize<ProbeRequest>(line, jsonOptions);
                if (req == null) continue;
            }
            catch (Exception ex)
            {
                writer.WriteLine(JsonSerializer.Serialize(new ProbeResponse
                {
                    Id = "",
                    Ok = false,
                    Error = $"Invalid JSON: {ex.Message}"
                }, jsonOptions));
                continue;
            }

            ProbeResponse resp;
            try
            {
                resp = Handle(req, window);
            }
            catch (Exception ex)
            {
                resp = new ProbeResponse { Id = req.Id, Ok = false, Error = ex.ToString() };
            }

            writer.WriteLine(JsonSerializer.Serialize(resp, jsonOptions));
        }
    }

    private static ProbeResponse Handle(ProbeRequest req, ProbeWindow window)
    {
        return req.Type switch
        {
            "ping" => Ok(req, new { ts = DateTimeOffset.Now }),
            "focus" => Ok(req, RunUi(window, window.EnsureForegroundAndFocus)),
            "clear" => Ok(req, RunUi(window, window.ClearState)),
            "snapshot" => Ok(req, window.Snapshot()),
            "shutdown" => Ok(req, RunUi(window, () => window.Close())),
            _ => new ProbeResponse { Id = req.Id, Ok = false, Error = $"Unknown request type: {req.Type}" }
        };
    }

    private static object RunUi(ProbeWindow window, Action action)
    {
        if (window.InvokeRequired)
        {
            window.Invoke(action);
        }
        else
        {
            action();
        }
        return new { ok = true };
    }

    private static ProbeResponse Ok(ProbeRequest req, object payload) => new()
    {
        Id = req.Id,
        Ok = true,
        Payload = payload
    };

    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }
}

