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
    var svc = new DockerDotNetRunnerService(workingDir, null);
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

                Console.WriteLine("RunnerCli: calling StartContainersAsync...");
                var started = await svc.StartContainersAsync(env, cts.Token);
                Console.WriteLine($"RunnerCli: StartContainersAsync returned {started}");
                if (!started)
                {
                    Console.WriteLine("RunnerCli: StartContainersAsync failed — aborting start sequence");
                    return 1;
                }

                Console.WriteLine("RunnerCli: calling RegisterAsync...");
                var registered = await svc.RegisterAsync(token, repo, url, cts.Token);
                Console.WriteLine($"RunnerCli: RegisterAsync returned {registered}");
                if (!registered)
                {
                    Console.WriteLine("RunnerCli: RegisterAsync failed; attempting to stop containers...");
                    await svc.StopContainersAsync(cts.Token);
                    return 1;
                }

                Console.WriteLine("RunnerCli: start sequence complete");
                return 0;
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
