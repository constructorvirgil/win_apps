using System.IO.Pipes;
using System.Text.Json;
using drvercheck.Probe;

namespace drvercheck.Runner;

internal sealed class PipeClient : IAsyncDisposable
{
    private readonly NamedPipeClientStream _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private PipeClient(NamedPipeClientStream client)
    {
        _client = client;
        _reader = new StreamReader(_client);
        _writer = new StreamWriter(_client) { AutoFlush = true };
    }

    public static async Task<PipeClient> ConnectAsync(string pipeName, TimeSpan timeout)
    {
        var start = DateTimeOffset.Now;
        Exception? last = null;

        while (DateTimeOffset.Now - start < timeout)
        {
            try
            {
                var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await client.ConnectAsync(3000);
                return new PipeClient(client);
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(200);
            }
        }

        throw new TimeoutException($"Failed to connect to pipe '{pipeName}' within {timeout}. Last error: {last?.Message}");
    }

    public async Task<ProbeResponse> SendAsync(string type, object? payload = null)
    {
        var req = new ProbeRequest { Id = Guid.NewGuid().ToString("N"), Type = type, Payload = payload };
        var line = JsonSerializer.Serialize(req, _jsonOptions);
        await _writer.WriteLineAsync(line);

        var respLine = await _reader.ReadLineAsync();
        if (respLine == null) throw new IOException("Probe disconnected.");

        var resp = JsonSerializer.Deserialize<ProbeResponse>(respLine, _jsonOptions);
        if (resp == null) throw new IOException("Invalid probe response.");
        return resp;
    }

    public async Task<ProbeSnapshot> SnapshotAsync()
    {
        var resp = await SendAsync("snapshot");
        if (!resp.Ok) throw new InvalidOperationException(resp.Error ?? "snapshot failed");

        // Payload is JsonElement when deserialized into object; re-serialize and parse strongly.
        var json = JsonSerializer.Serialize(resp.Payload, _jsonOptions);
        return JsonSerializer.Deserialize<ProbeSnapshot>(json, _jsonOptions)!;
    }

    public async ValueTask DisposeAsync()
    {
        try { _writer.Dispose(); } catch { }
        try { _reader.Dispose(); } catch { }
        try { _client.Dispose(); } catch { }
        await Task.CompletedTask;
    }
}

