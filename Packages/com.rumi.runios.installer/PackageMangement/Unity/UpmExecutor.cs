#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEngine;

namespace RuniOS.PackageManagement.Unity
{
    /// <summary>
    /// Observes UPM requirements, batches missing references, and verifies the resulting native state.<br/>
    /// UPM 요구사항을 관측하고 누락된 참조를 batch 처리한 뒤 native 결과 상태를 확인합니다.
    /// </summary>
    /// <remarks>
    /// Satisfaction predicates are evaluated on the main thread.<br/>
    /// 만족 여부 predicate는 main thread에서 평가합니다.
    /// </remarks>
    public sealed class UpmExecutor : IInstallationExecutor<UpmInstallation>
    {
        /// <inheritdoc/>
        public async IAsyncEnumerable<InstallationResult> EnsureAsync(IEnumerable<UpmInstallation> installations, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (installations is null) throw new ArgumentNullException(nameof(installations));
            UpmInstallation[] batch = installations.ToArray();
            if (batch.Length == 0) yield break;
            InstallationResult[] results = await EnsureBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            foreach (InstallationResult result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return result;
            }
        }
        static async Task<InstallationResult[]> EnsureBatchAsync(UpmInstallation[] batch, CancellationToken cancellationToken)
        {
            try
            {
                var targets = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (UpmInstallation installation in batch)
                {
                    if (targets.TryGetValue(installation.packageName, out string? previous) && previous != installation.packageReference)
                        throw new InvalidOperationException($"Conflicting UPM references target '{installation.packageName}': '{previous}' and '{installation.packageReference}'.");
                    targets[installation.packageName] = installation.packageReference;
                }
                PackageInfo[] current = await UnityEditorThread.RunAsync(() => PackageInfo.GetAllRegisteredPackages(), cancellationToken).ConfigureAwait(false);
                await Awaitable.MainThreadAsync();
                var pending = new List<UpmInstallation>();
                foreach (UpmInstallation installation in batch)
                    if (!Matches(installation, current)) pending.Add(installation);
                if (pending.Count != 0)
                {
                    await UnityEditorThread.RunAsync(() => { ApplyRegistries(pending, cancellationToken); return true; }, cancellationToken).ConfigureAwait(false);
                    string[] additions = pending.Select(x => x.packageReference).Distinct(StringComparer.Ordinal).ToArray();
                    await UnityUpmClient.RunAsync(() => Client.AddAndRemove(additions, Array.Empty<string>()), cancellationToken).ConfigureAwait(false);
                    // A batch may affect previously satisfied requirements. Verify the whole requested set.
                    current = (await UnityUpmClient.RunAsync(() => Client.List(true, true), cancellationToken).ConfigureAwait(false)).ToArray();
                }
                await Awaitable.MainThreadAsync();
                var results = new InstallationResult[batch.Length];
                for (int i = 0; i < batch.Length; i++)
                    results[i] = Matches(batch[i], current) ? new InstallationResult(batch[i], true) : new InstallationResult(batch[i], false, new[]
                    {
                        new InstallationDiagnostic("upm:not-satisfied", $"UPM did not satisfy '{batch[i].packageName}' using '{batch[i].packageReference}'.")
                    });
                return results;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var results = new InstallationResult[batch.Length];
                for (int i = 0; i < batch.Length; i++)
                    results[i] = new InstallationResult(batch[i], false, new[] { new InstallationDiagnostic("upm:execution-failed", exception.Message) });
                return results;
            }
        }
        static bool Matches(UpmInstallation installation, IEnumerable<PackageInfo> infos)
        {
            foreach (PackageInfo info in infos)
                if (StringComparer.Ordinal.Equals(info.name, installation.packageName))
                    return (info.errors is null || info.errors.Length == 0) && installation.isSatisfied(info);
            return false;
        }
        static void ApplyRegistries(IEnumerable<UpmInstallation> installations, CancellationToken cancellationToken)
        {
            ScopedRegistryDefinition[] requirements = installations.Select(x => x.registry).OfType<ScopedRegistryDefinition>().ToArray();
            if (requirements.Length == 0) return;
            string manifestPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Packages", "manifest.json");
            using var reader = new JsonTextReader(new StringReader(File.ReadAllText(manifestPath)));
            reader.DateParseHandling = DateParseHandling.None;
            reader.MaxDepth = 128;
            JObject manifest = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            while (reader.Read())
                if (reader.TokenType != JsonToken.Comment) throw new FormatException("The manifest contains trailing JSON content.");
            if (manifest["scopedRegistries"] is not null && manifest["scopedRegistries"] is not JArray)
                throw new FormatException("The manifest scopedRegistries field must be an array.");
            var entries = (JArray?)manifest["scopedRegistries"] ?? new JArray();
            bool changed = false;
            foreach (ScopedRegistryDefinition requirement in requirements)
            {
                JObject? existing = null;
                foreach (JToken token in entries)
                {
                    if (token is not JObject entry || entry["url"]?.Type != JTokenType.String)
                        throw new FormatException("A scoped registry must contain a string URL.");
                    string url = ((string)entry["url"]!).TrimEnd('/');
                    if ((string?)entry["name"] == requirement.name && url != requirement.url)
                        throw new InvalidOperationException($"Scoped registry '{requirement.name}' already uses a different URL.");
                    if (url == requirement.url) existing = entry;
                }
                if (existing is null)
                {
                    entries.Add(new JObject { ["name"] = requirement.name, ["url"] = requirement.url, ["scopes"] = new JArray(requirement.scopes) });
                    changed = true;
                    continue;
                }
                if (existing["scopes"] is not JArray scopes) throw new FormatException("A scoped registry must contain a scopes array.");
                foreach (string scope in requirement.scopes)
                {
                    bool found = false;
                    foreach (JToken token in scopes)
                    {
                        if (token.Type != JTokenType.String) throw new FormatException("Registry scopes must be strings.");
                        if ((string?)token == scope) found = true;
                    }
                    if (!found) { scopes.Add(scope); changed = true; }
                }
            }
            if (!changed) return;
            cancellationToken.ThrowIfCancellationRequested();
            manifest["scopedRegistries"] = entries;
            // Only scopedRegistries is edited. Native UPM owns dependencies and the lock file.
            File.WriteAllText(manifestPath, manifest.ToString(Formatting.Indented) + "\n");
        }
    }
}
