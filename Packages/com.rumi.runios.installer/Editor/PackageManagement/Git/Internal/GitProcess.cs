#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace RuniOS.PackageManagement.Unity.Editor.Git.Internal
{
    internal static class GitProcess
    {
        internal static async Task<string> RunAsync(string directory, IEnumerable<string> arguments, CancellationToken cancellationToken)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            ProcessStartInfo start = new("git")
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
                WorkingDirectory = directory, Arguments = string.Join(" ", arguments.Select(Quote)),
                StandardOutputEncoding = new System.Text.UTF8Encoding(false, true), StandardErrorEncoding = System.Text.Encoding.UTF8
            };
            start.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
            start.EnvironmentVariables["GIT_LFS_SKIP_SMUDGE"] = "1";
            using Process process = new() { StartInfo = start };
            process.Start();
            using CancellationTokenRegistration registration = timeout.Token.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(); } catch (InvalidOperationException) { } catch (System.ComponentModel.Win32Exception) { }
            });
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> errors = process.StandardError.ReadToEndAsync();
            // Disposing streams after a killed process may fault an unread task.
            _ = output.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            _ = errors.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            await Task.Run(() => process.WaitForExit()).ConfigureAwait(false);
            if (timeout.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new IOException("Git timed out. Check network access and preconfigured Git authentication.");
            }
            if (process.ExitCode != 0) throw new IOException($"Git failed with exit code {process.ExitCode}. Check repository access and the requested reference.");
            string result = await output.ConfigureAwait(false);
            await errors.ConfigureAwait(false);
            return result;
        }
        static string Quote(string value)
        {
            // ProcessStartInfo argument quoting; no command shell participates.
            string escaped = System.Text.RegularExpressions.Regex.Replace(value, @"(\\*)""", "$1$1\\\"");
            escaped = System.Text.RegularExpressions.Regex.Replace(escaped, @"(\\+)$", "$1$1");
            return "\"" + escaped + "\"";
        }
    }
}
