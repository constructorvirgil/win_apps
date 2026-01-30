using System.Text.Json.Serialization;

namespace drvercheck.Probe;

public sealed class ProbeRequest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}

public sealed class ProbeResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public sealed class ProbeSnapshot
{
    public string Text { get; set; } = "";
    public DateTimeOffset CapturedAt { get; set; }
    public List<ProbeEvent> Events { get; set; } = new();
    public ProbeRect WindowRect { get; set; }
    public ProbePoint TargetPoint { get; set; }
    public long WindowHandle { get; set; }
    public int ProcessId { get; set; }
}

public sealed class ProbeEvent
{
    public DateTimeOffset Ts { get; set; }
    public string Kind { get; set; } = "";
    public uint Msg { get; set; }
    public long WParam { get; set; }
    public long LParam { get; set; }
}

public readonly record struct ProbePoint(int X, int Y);
public readonly record struct ProbeRect(int Left, int Top, int Right, int Bottom);
