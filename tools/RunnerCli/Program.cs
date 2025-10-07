using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.RunnerTasks;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: dotnet run --project tools/RunnerCli -- start|stop --repo <owner/repo> --token <regToken> [--url <githubUrl>]");
            return 2;
        }

        var cmd = args[0].ToLowerInvariant();
        var repo = GetArgValue(args, "--repo") ?? Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
        var token = GetArgValue(args, "--token") ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        var url = GetArgValue(args, "--url") ?? Environment.GetEnvironmentVariable("GITHUB_URL") ?? "https://github.com";

        if (string.IsNullOrEmpty(repo))
        {
            Console.Error.WriteLine("Missing repo (use --repo or set GITHUB_REPOSITORY)");
            return 2;
        }

    var workingDir = System.IO.Path.GetFullPath("src/GitHub.RunnerTasks");
    Console.WriteLine($"RunnerCli: cmd={cmd}, repo={repo}, url={url}");
    Console.WriteLine($"RunnerCli: token present={(string.IsNullOrEmpty(token) ? "no" : "yes")}, token masked={(string.IsNullOrEmpty(token) ? "" : token.Substring(0,4) + new string('*', Math.Max(0, token.Length-8)) + token.Substring(Math.Max(4, token.Length-4)))}");
    await using var svc = new DockerRunnerService(null);
    var manager = new RunnerManager(svc, null);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        try
        {
            if (cmd == "start")
            {
                if (string.IsNullOrEmpty(token))
                {
                    Console.Error.WriteLine("Missing registration token (use --token or set GITHUB_TOKEN)");
                    return 2;
                }

                var env = new[] { $"GITHUB_REPOSITORY={repo}", $"GITHUB_TOKEN={token}", $"GITHUB_URL={url}" };

                Console.WriteLine("RunnerCli: orchestrating start (register + start containers)...");
                var ok = await manager.OrchestrateStartAsync(token!, repo!, url!, env, maxRetries: 5, baseDelayMs: 200, cancellationToken: cts.Token);
                Console.WriteLine($"RunnerCli: OrchestrateStartAsync returned {ok}");
                return ok ? 0 : 1;
            }
            else if (cmd == "stop")
            {
                var ok = await manager.OrchestrateStopAsync(cts.Token);
                return ok ? 0 : 1;
            }
            else
            {
                Console.Error.WriteLine($"Unknown command: {cmd}");
                return 2;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"RunnerCli failed: {ex}");
            return 1;
        }
    }

    static string? GetArgValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) return args[i + 1];
        }
        return null;
    }

    // using Microsoft.Extensions.Logging.Console for output
}
