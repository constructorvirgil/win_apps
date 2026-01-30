namespace drvercheck.Runner;

internal sealed record TestResult(string Name, string Status, string? Message = null);

internal sealed class TestReport
{
    public string Driver { get; set; } = "";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset FinishedAt { get; set; }
    public List<TestResult> Results { get; set; } = new();

    public int FailedCount => Results.Count(r => r.Status == "fail");
    public int SkippedCount => Results.Count(r => r.Status == "skip");
    public int PassedCount => Results.Count(r => r.Status == "pass");
}

