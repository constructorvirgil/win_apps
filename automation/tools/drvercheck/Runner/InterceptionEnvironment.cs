using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace drvercheck.Runner;

internal static class InterceptionEnvironment
{
    internal sealed record KernelStatus(bool KeyboardServiceExists, bool MouseServiceExists, string? KeyboardState, string? MouseState);

    public static KernelStatus GetKernelDriverStatus()
    {
        var keyboard = QueryService("keyboard");
        var mouse = QueryService("mouse");

        return new KernelStatus(
            KeyboardServiceExists: keyboard.exists,
            MouseServiceExists: mouse.exists,
            KeyboardState: keyboard.state,
            MouseState: mouse.state);
    }

    public static bool TryLoadInterceptionDll(out string? error)
    {
        error = null;
        try
        {
            if (NativeLibrary.TryLoad("interception.dll", out var handle))
            {
                NativeLibrary.Free(handle);
                return true;
            }

            error = "NativeLibrary.TryLoad returned false.";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static string? EnsureInterceptionDllPresent(string? preferredConfiguration)
    {
        var baseDir = AppContext.BaseDirectory;
        var target = Path.Combine(baseDir, "interception.dll");
        if (File.Exists(target)) return target;

        var candidates = FindCandidateDllPaths(preferredConfiguration).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var source = candidates.FirstOrDefault(File.Exists);
        if (source == null) return null;

        Directory.CreateDirectory(baseDir);
        File.Copy(source, target, overwrite: true);
        return target;
    }

    private static IEnumerable<string> FindCandidateDllPaths(string? preferredConfiguration)
    {
        var baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(Environment.CurrentDirectory, "interception.dll");

        // Common locations if caller already built the app/tool.
        var repoRoot = TryFindRepoRoot(baseDir);
        if (repoRoot != null)
        {
            foreach (var cfg in CandidateConfigurations(preferredConfiguration))
            {
                // App output
                yield return Path.Combine(repoRoot, "src", "VirtualHidSimulator.App", "bin", cfg, "net8.0-windows", "interception.dll");
                yield return Path.Combine(repoRoot, "src", "VirtualHidSimulator.App", "bin", cfg, "net8.0-windows", "publish", "interception.dll");

                // Legacy/root output (in case older layout produced it)
                yield return Path.Combine(repoRoot, "bin", cfg, "net8.0-windows", "interception.dll");
                yield return Path.Combine(repoRoot, "bin", cfg, "net8.0-windows", "publish", "interception.dll");
            }

            // As a last resort, scan limited folders only (avoid full-tree scan).
            foreach (var path in SafeEnumerate(repoRoot, Path.Combine("src", "VirtualHidSimulator.App", "bin"), "interception.dll"))
                yield return path;
            foreach (var path in SafeEnumerate(repoRoot, "bin", "interception.dll"))
                yield return path;
        }

        // If launched from build output folder, try sibling layout.
        foreach (var cfg in CandidateConfigurations(preferredConfiguration))
        {
            // tools\drvercheck\bin\<cfg>\net8.0-windows -> repo\src\VirtualHidSimulator.App\bin\<cfg>\net8.0-windows
            var up = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", ".."));
            yield return Path.Combine(up, "src", "VirtualHidSimulator.App", "bin", cfg, "net8.0-windows", "interception.dll");
        }
    }

    private static IEnumerable<string> CandidateConfigurations(string? preferred)
    {
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            yield return preferred!;
        }
        yield return "Debug";
        yield return "Release";
    }

    private static string? TryFindRepoRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "automation.slnx"))) return dir.FullName;
            if (File.Exists(Path.Combine(dir.FullName, "src", "VirtualHidSimulator.App", "VirtualHidSimulator.App.csproj"))) return dir.FullName;
        }
        return null;
    }

    private static IEnumerable<string> SafeEnumerate(string repoRoot, string subdir, string fileName)
    {
        var results = new List<string>();
        try
        {
            var dir = Path.Combine(repoRoot, subdir);
            if (!Directory.Exists(dir)) return results;
            foreach (var f in Directory.EnumerateFiles(dir, fileName, SearchOption.AllDirectories))
            {
                results.Add(f);
            }
        }
        catch
        {
            return results;
        }

        return results;
    }

    private static (bool exists, string? state) QueryService(string name)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"query {name}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return (false, null);
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(3000);

            // If service doesn't exist, sc prints FAILED 1060 and exit code is non-zero.
            if (p.ExitCode != 0) return (false, null);

            var stateLine = output.Split('\n')
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.StartsWith("STATE", StringComparison.OrdinalIgnoreCase));

            var state = stateLine == null ? null : stateLine;
            return (true, state);
        }
        catch
        {
            return (false, null);
        }
    }
}
