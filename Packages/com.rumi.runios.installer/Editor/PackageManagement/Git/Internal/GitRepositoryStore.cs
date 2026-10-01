#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;

namespace RuniOS.PackageManagement.Unity.Editor.Git.Internal
{
    internal sealed class GitRepositoryStore
    {
        static readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new(StringComparer.Ordinal);
        readonly string cacheRoot;
        internal GitRepositoryStore(string cacheRoot) => this.cacheRoot = Path.GetFullPath(cacheRoot);
        string DirectoryFor(string repository)
        {
            using var algorithm = System.Security.Cryptography.SHA256.Create();
            string key = BitConverter.ToString(algorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(repository))).Replace("-", "").ToLowerInvariant();
            return Path.Combine(cacheRoot, key);
        }
        internal async Task<string> ResolveAsync(string repository, string reference, CancellationToken cancellationToken)
        {
            new GitReferenceConstraint(reference);
            string directory = DirectoryFor(repository);
            SemaphoreSlim gate = locks.GetOrAdd(directory, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Directory.CreateDirectory(directory);
                if (!File.Exists(Path.Combine(directory, "HEAD")))
                    await GitProcess.RunAsync(directory, new[] { "init", "--bare", "." }, cancellationToken).ConfigureAwait(false);
                bool exact = System.Text.RegularExpressions.Regex.IsMatch(reference, @"^(?:[0-9a-f]{40}|[0-9a-f]{64})$");
                if (exact)
                {
                    try { return (await GitProcess.RunAsync(directory, new[] { "rev-parse", "--verify", reference + "^{commit}" }, cancellationToken).ConfigureAwait(false)).Trim(); }
                    catch (IOException) { cancellationToken.ThrowIfCancellationRequested(); }
                }
                await GitProcess.RunAsync(directory, new[] { "fetch", "--no-tags", "--no-auto-gc", "--no-recurse-submodules", "--depth=1", "--", repository, reference }, cancellationToken).ConfigureAwait(false);
                return (await GitProcess.RunAsync(directory, new[] { "rev-parse", "--verify", "FETCH_HEAD^{commit}" }, cancellationToken).ConfigureAwait(false)).Trim();
            }
            finally { gate.Release(); }
        }
        internal async Task<string?> ReadAsync(GitCommitArtifact artifact, string filename, CancellationToken cancellationToken)
        {
            string path = (artifact.source.packagePath.Length == 0 ? "" : artifact.source.packagePath + "/") + filename;
            string directory = DirectoryFor(artifact.source.repositoryUrl);
            string tree = await GitProcess.RunAsync(directory, new[] { "ls-tree", artifact.commit, "--", path }, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(tree)) return null;
            if (!tree.StartsWith("100644 blob ", StringComparison.Ordinal) && !tree.StartsWith("100755 blob ", StringComparison.Ordinal))
                throw new IOException("Package metadata must be a regular Git blob, not a symbolic link or submodule.");
            string length = await GitProcess.RunAsync(directory, new[] { "cat-file", "-s", artifact.commit + ":" + path }, cancellationToken).ConfigureAwait(false);
            if (!long.TryParse(length.Trim(), out long size) || size < 0 || size > 4 * 1024 * 1024)
                throw new IOException("The Git package metadata exceeds the retrieval limit.");
            return await GitProcess.RunAsync(directory, new[] { "show", artifact.commit + ":" + path }, cancellationToken).ConfigureAwait(false);
        }
    }
}
