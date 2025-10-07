using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.RunnerTasks
{
    internal static class ProcessRunner
    {
        // Runs a process and streams stdout/stderr to Console. Returns exit code or -1 on failure.
        public static async Task<int> RunAndStreamAsync(string fileName, string arguments, string? workingDirectory, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDirectory ?? string.Empty,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return -1;

            try
            {
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
                try { proc.BeginOutputReadLine(); proc.BeginErrorReadLine(); } catch { }
                await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                return proc.ExitCode;
            }
            catch (OperationCanceledException)
            {
                try { if (!proc.HasExited) proc.Kill(true); } catch { }
                return -1;
            }
            catch
            {
                try { if (!proc.HasExited) proc.Kill(true); } catch { }
                return -1;
            }
        }

        // Runs a process and returns stdout (or stderr if non-zero exit). Returns null if the process couldn't start or was canceled/failed to execute.
        public static async Task<string?> CaptureOutputAsync(string fileName, string arguments, string? workingDirectory, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDirectory ?? string.Empty,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return null;

            try
            {
                var outStr = await proc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                var errStr = await proc.StandardError.ReadToEndAsync().ConfigureAwait(false);
                await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                return proc.ExitCode == 0 ? outStr : (string.IsNullOrEmpty(outStr) ? errStr : outStr);
            }
            catch (OperationCanceledException)
            {
                try { if (!proc.HasExited) proc.Kill(true); } catch { }
                return null;
            }
            catch
            {
                try { if (!proc.HasExited) proc.Kill(true); } catch { }
                return null;
            }
        }
    }
}
