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
    /// Ensures native package names and registry requirements, batching references only for absent packages.<br/>
    /// native package 이름과 registry 요구사항을 ensure하고 없는 package의 참조만 batch 처리합니다.
    /// </summary>
    /// <remarks>
    /// Any registered package with the requested name satisfies the package requirement, regardless of source, version, commit, path, or native errors.<br/>
    /// Required assembly-definition assets are checked independently; an unmet requirement blocks the entire UPM batch before registry changes or package requests.
    /// <br/><br/>
    /// 요청한 이름의 등록된 package가 있으면 source, version, commit, path 또는 native 오류와 무관하게 package 요구사항을 충족합니다.<br/>
    /// 필수 어셈블리 정의 에셋은 별도로 검사하며 미충족 요구사항은 registry 변경이나 package 요청 전에 UPM batch 전체를 차단합니다.
    /// </remarks>
    public sealed class UpmExecutor : IInstallationExecutor<UpmInstallation>, IInstallationPreviewer<UpmInstallation>
    {
        /// <inheritdoc/>
        public async IAsyncEnumerable<InstallationPreview> PreviewAsync(IEnumerable<UpmInstallation> installations, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (installations is null) throw new ArgumentNullException(nameof(installations));
            UpmInstallation[] batch = installations.ToArray();
            if (batch.Length == 0) yield break;
            InstallationPreview[] previews = await PreviewBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            foreach (InstallationPreview preview in previews)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return preview;
            }
        }
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
        static async Task<InstallationPreview[]> PreviewBatchAsync(UpmInstallation[] batch, CancellationToken cancellationToken)
        {
            try
            {
                return await UnityEditorThread.RunAsync(() =>
                {
                    InstallationDiagnostic[][] assemblyDiagnostics = ObserveAssemblyRequirements(batch, cancellationToken);
                    PackageInfo[] current = batch.Any(x => x.ensurePackage) ? PackageInfo.GetAllRegisteredPackages() : Array.Empty<PackageInfo>();
                    ValidatePackageReferences(batch, current);
                    bool[] registries = ObserveRegistries(batch, cancellationToken);
                    var previews = new InstallationPreview[batch.Length];
                    for (int i = 0; i < batch.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (assemblyDiagnostics[i].Length != 0)
                        {
                            previews[i] = new InstallationPreview(batch[i], InstallationPreviewStatus.Failed, assemblyDiagnostics[i]);
                            continue;
                        }
                        InstallationPreviewStatus status = !registries[i] ? InstallationPreviewStatus.RequiresEnsure
                            : !batch[i].ensurePackage ? InstallationPreviewStatus.Delegated
                            : IsInstalled(batch[i], current) ? InstallationPreviewStatus.Satisfied : InstallationPreviewStatus.RequiresEnsure;
                        previews[i] = new InstallationPreview(batch[i], status);
                    }
                    return previews;
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var previews = new InstallationPreview[batch.Length];
                for (int i = 0; i < batch.Length; i++)
                    previews[i] = new InstallationPreview(batch[i], InstallationPreviewStatus.Failed, new[] { new InstallationDiagnostic("upm:preview-failed", exception.Message) });
                return previews;
            }
        }
        static async Task<InstallationResult[]> EnsureBatchAsync(UpmInstallation[] batch, CancellationToken cancellationToken)
        {
            try
            {
                InstallationDiagnostic[][] assemblyDiagnostics = await UnityEditorThread.RunAsync(() => ObserveAssemblyRequirements(batch, cancellationToken), cancellationToken).ConfigureAwait(false);
                bool blocked = false;
                foreach (InstallationDiagnostic[] diagnostics in assemblyDiagnostics)
                    if (diagnostics.Length != 0) { blocked = true; break; }
                if (blocked)
                {
                    var blockedResults = new InstallationResult[batch.Length];
                    for (int i = 0; i < batch.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        blockedResults[i] = new InstallationResult(batch[i], false, assemblyDiagnostics[i].Length != 0 ? assemblyDiagnostics[i] : new[]
                        {
                            new InstallationDiagnostic("upm:batch-blocked", "The UPM batch was not started because a required assembly-definition asset is missing. Resolve the missing references before retrying.")
                        });
                    }
                    return blockedResults;
                }
                PackageInfo[] current = await UnityEditorThread.RunAsync(() => batch.Any(x => x.ensurePackage) ? PackageInfo.GetAllRegisteredPackages() : Array.Empty<PackageInfo>(), cancellationToken).ConfigureAwait(false);
                ValidatePackageReferences(batch, current);
                // Registry requirements belong to the entire batch, including delegated and already-installed packages.
                await UnityEditorThread.RunAsync(() => { ApplyRegistries(batch, cancellationToken); return true; }, cancellationToken).ConfigureAwait(false);
                await Awaitable.MainThreadAsync();
                var pending = new List<UpmInstallation>();
                foreach (UpmInstallation installation in batch)
                    if (installation.ensurePackage && !IsInstalled(installation, current)) pending.Add(installation);
                if (pending.Count != 0)
                {
                    string[] additions = pending.Select(x => x.packageReference).Distinct(StringComparer.Ordinal).ToArray();
                    await UnityUpmClient.RunAsync(() => Client.AddAndRemove(additions, Array.Empty<string>()), cancellationToken).ConfigureAwait(false);
                    // A batch may affect previously satisfied requirements. Verify the whole requested set.
                    current = (await UnityUpmClient.RunAsync(() => Client.List(true, true), cancellationToken).ConfigureAwait(false)).ToArray();
                }
                await Awaitable.MainThreadAsync();
                bool[] registries = ObserveRegistries(batch, cancellationToken);
                var results = new InstallationResult[batch.Length];
                for (int i = 0; i < batch.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!registries[i])
                        results[i] = new InstallationResult(batch[i], false, new[] { new InstallationDiagnostic("upm:registry-not-satisfied", $"Required registry configuration for '{batch[i].packageName}' is missing.") });
                    else results[i] = !batch[i].ensurePackage || IsInstalled(batch[i], current) ? new InstallationResult(batch[i], true) : new InstallationResult(batch[i], false, new[]
                    {
                        new InstallationDiagnostic("upm:not-satisfied", $"UPM package '{batch[i].packageName}' is not registered after requesting '{batch[i].packageReference}'.")
                    });
                }
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
        static InstallationDiagnostic[][] ObserveAssemblyRequirements(UpmInstallation[] batch, CancellationToken cancellationToken)
        {
            var diagnostics = new InstallationDiagnostic[batch.Length][];
            for (int i = 0; i < batch.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                diagnostics[i] = AssemblyRequirementUtility.Observe(batch[i].packageName, batch[i].requiredAssemblyReferences);
            }
            return diagnostics;
        }
        static void ValidatePackageReferences(IEnumerable<UpmInstallation> installations, IEnumerable<PackageInfo> current)
        {
            var targets = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (UpmInstallation installation in installations)
            {
                if (!installation.ensurePackage || IsInstalled(installation, current)) continue;
                if (targets.TryGetValue(installation.packageName, out string? previous) && previous != installation.packageReference)
                    throw new InvalidOperationException($"Conflicting UPM references target '{installation.packageName}': '{previous}' and '{installation.packageReference}'.");
                targets[installation.packageName] = installation.packageReference;
            }
        }
        static bool IsInstalled(UpmInstallation installation, IEnumerable<PackageInfo> infos)
        {
            foreach (PackageInfo info in infos)
                if (StringComparer.Ordinal.Equals(info.name, installation.packageName))
                    return true;
            return false;
        }
        static void ApplyRegistries(IEnumerable<UpmInstallation> installations, CancellationToken cancellationToken)
        {
            ScopedRegistryDefinition[] requirements = installations.Select(x => x.registry).OfType<ScopedRegistryDefinition>().ToArray();
            if (requirements.Length == 0) return;
            JObject manifest = ReadManifest(out string manifestPath);
            if (!MergeRegistries(manifest, requirements, cancellationToken)) return;
            cancellationToken.ThrowIfCancellationRequested();
            // Only scopedRegistries is edited. Native UPM owns dependencies and the lock file.
            File.WriteAllText(manifestPath, manifest.ToString(Formatting.Indented) + "\n");
        }
        static bool[] ObserveRegistries(UpmInstallation[] batch, CancellationToken cancellationToken)
        {
            var satisfied = new bool[batch.Length];
            ScopedRegistryDefinition[] requirements = batch.Select(x => x.registry).OfType<ScopedRegistryDefinition>().ToArray();
            if (requirements.Length == 0)
            {
                Array.Fill(satisfied, true);
                return satisfied;
            }
            JObject manifest = ReadManifest(out _);
            JArray entries = RegistryEntries(manifest);
            for (int i = 0; i < batch.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ScopedRegistryDefinition? requirement = batch[i].registry;
                if (requirement is null) { satisfied[i] = true; continue; }
                JObject? existing = FindRegistry(entries, requirement);
                if (existing is null) continue;
                if (existing["scopes"] is not JArray scopes) throw new FormatException("A scoped registry must contain a scopes array.");
                satisfied[i] = true;
                foreach (string scope in requirement.scopes)
                    if (!ContainsScope(scopes, scope)) satisfied[i] = false;
            }
            // Validate requirements together using temporary JSON; observations above refer only to the current manifest.
            // This temporary merge is never written or retained for Ensure.
            MergeRegistries(manifest, requirements, cancellationToken);
            return satisfied;
        }
        static JObject ReadManifest(out string manifestPath)
        {
            manifestPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Packages", "manifest.json");
            using var reader = new JsonTextReader(new StringReader(File.ReadAllText(manifestPath)));
            reader.DateParseHandling = DateParseHandling.None;
            reader.MaxDepth = 128;
            JObject manifest = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            while (reader.Read())
                if (reader.TokenType != JsonToken.Comment) throw new FormatException("The manifest contains trailing JSON content.");
            return manifest;
        }
        static JArray RegistryEntries(JObject manifest)
        {
            if (manifest["scopedRegistries"] is not null && manifest["scopedRegistries"] is not JArray)
                throw new FormatException("The manifest scopedRegistries field must be an array.");
            return (JArray?)manifest["scopedRegistries"] ?? new JArray();
        }
        static JObject? FindRegistry(JArray entries, ScopedRegistryDefinition requirement)
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
            return existing;
        }
        static bool ContainsScope(JArray scopes, string scope)
        {
            bool found = false;
            foreach (JToken token in scopes)
            {
                if (token.Type != JTokenType.String) throw new FormatException("Registry scopes must be strings.");
                if ((string?)token == scope) found = true;
            }
            return found;
        }
        static bool MergeRegistries(JObject manifest, IEnumerable<ScopedRegistryDefinition> requirements, CancellationToken cancellationToken)
        {
            JArray entries = RegistryEntries(manifest);
            bool changed = false;
            foreach (ScopedRegistryDefinition requirement in requirements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                JObject? existing = FindRegistry(entries, requirement);
                if (existing is null)
                {
                    entries.Add(new JObject { ["name"] = requirement.name, ["url"] = requirement.url, ["scopes"] = new JArray(requirement.scopes) });
                    changed = true;
                    continue;
                }
                if (existing["scopes"] is not JArray scopes) throw new FormatException("A scoped registry must contain a scopes array.");
                foreach (string scope in requirement.scopes)
                {
                    if (!ContainsScope(scopes, scope)) { scopes.Add(scope); changed = true; }
                }
            }
            if (changed) manifest["scopedRegistries"] = entries;
            return changed;
        }
    }
}
