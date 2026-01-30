using drvercheck.Probe;
using drvercheck.Runner;

namespace drvercheck;

internal static class Program
{
    [STAThread]
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine(
                """
                drvercheck - driver verification CLI

                Commands:
                  drvercheck probe --pipe <name>
                  drvercheck selftest --driver <SendInput|Interception> [--pipe <name>] [--json]

                Notes:
                  - selftest will spawn a probe window and inject input into it.
                  - Interception requires interception.dll + driver installed (admin + reboot usually).
                """);
            return 0;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "probe" => ProbeHost.Run(args.Skip(1).ToArray()),
                "selftest" => await SelfTestCommand.RunAsync(args.Skip(1).ToArray()),
                _ => Fail($"Unknown command: {args[0]}")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Run `drvercheck --help` for usage.");
        return 2;
    }
}

