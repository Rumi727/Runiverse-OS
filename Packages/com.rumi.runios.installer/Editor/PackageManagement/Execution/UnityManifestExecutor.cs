#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Planning;
using RuniOS.PackageManagement.Providers;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using RuniOS.PackageManagement.Unity.Editor.Internal;
using RuniOS.PackageManagement.Unity.Editor.Internal.Json;
using RuniOS.PackageManagement.Unity.Editor.Manifest;
using RuniOS.PackageManagement.Unity.Editor.Git;
using RuniOS.PackageManagement.Unity.Editor.State;
using RuniOS.PackageManagement.Unity.Editor.State.Internal;

namespace RuniOS.PackageManagement.Unity.Editor.Execution
{
    /// <summary>
    /// Checks preview preconditions, applies a composed manifest and verifies UPM resolution.<br/>
    /// preview 전제조건을 검사하고 합성된 manifest를 적용한 뒤 UPM 해석을 검증합니다.
    /// </summary>
    public sealed class UnityManifestExecutor : IInstallOperationExecutor
    {
        /// <inheritdoc/>
        public string operationKind => UnityManifestOperation.operationKind;
        /// <inheritdoc/>
        public async Task<PackageResult<OperationReceipt>> ExecuteAsync(InstallOperation operation, InstallationTarget target, CancellationToken cancellationToken)
        {
            if (operation is not UnityManifestOperation transaction || target is not UnityProjectTarget project)
                return AdapterData.Failure<OperationReceipt>("unity:invalid-manifest-operation", PackageDiagnosticPhase.Execution, "A Unity manifest operation and project target are required.");
            await UnityUpmClient.gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            bool applied = false, reloadLocked = false;
            string? journalPath = null;
            Dictionary<string, object?> journal = new(StringComparer.Ordinal);
            try
            {
                await UnityInstalledStateProvider.EnsureCurrentAsync(project, cancellationToken).ConfigureAwait(false);
                UnityProjectFiles.Check(project, transaction.preconditions);
                cancellationToken.ThrowIfCancellationRequested();
                await EditorThread.RunAsync(() => { EditorApplication.LockReloadAssemblies(); return true; }, cancellationToken).ConfigureAwait(false);
                reloadLocked = true;
                journalPath = Path.Combine(project.projectPath, "Library", "RuniOS.PackageManagement", "transactions", Guid.NewGuid().ToString("N") + ".json");
                var before = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (string resource in new[] { UnityProjectFiles.manifestResource, UnityProjectFiles.lockResource, UnityProjectFiles.ownershipResource })
                {
                    string path = UnityProjectFiles.PathFor(project, resource);
                    before[resource] = File.Exists(path) ? Convert.ToBase64String(File.ReadAllBytes(path)) : null;
                }
                journal["schemaVersion"] = new JsonNumber("1"); journal["status"] = "prepared";
                journal["before"] = before; journal["plannedManifest"] = transaction.manifestJson;
                UnityProjectFiles.Write(journalPath, JsonData.Write(journal, true));
                UnityProjectFiles.Check(project, transaction.preconditions);
                cancellationToken.ThrowIfCancellationRequested();
                // Publish registry/environment edits first. Let the native completion request apply
                // package additions/removals, including removals still present in the old manifest.
                var stagedManifest = JsonData.Parse(transaction.manifestJson);
                stagedManifest["dependencies"] = JsonData.Map(JsonData.Parse(File.ReadAllText(UnityProjectFiles.PathFor(project, UnityProjectFiles.manifestResource))), "dependencies");
                UnityProjectFiles.Check(project, transaction.preconditions);
                UnityProjectFiles.Write(UnityProjectFiles.PathFor(project, UnityProjectFiles.manifestResource), JsonData.Write(stagedManifest, true) + "\n");
                applied = true;
                journal["status"] = "manifest-applied"; UnityProjectFiles.Write(journalPath, JsonData.Write(journal, true));
                string[] additions = transaction.expectations.Select(x => x.packageIdentifier).OfType<string>().ToArray();
                string[] removals = transaction.removals.Select(x => x.value).ToArray();
                AddAndRemoveRequest request = await EditorThread.RunAsync(() => Client.AddAndRemove(additions, removals), CancellationToken.None).ConfigureAwait(false);
                await UnityUpmClient.WaitAsync(request, cancellationToken).ConfigureAwait(false);
                // Obtain the complete effective graph after the native batch has drained.
                PackageInfo[] infos = await UnityUpmClient.ListAsync(cancellationToken).ConfigureAwait(false);
                string editor = await EditorThread.RunAsync(() => Application.unityVersion, cancellationToken).ConfigureAwait(false);
                string manifestPath = UnityProjectFiles.PathFor(project, UnityProjectFiles.manifestResource);
                string actual = File.ReadAllText(manifestPath);
                var normalized = JsonData.Parse(actual);
                var normalizedDependencies = JsonData.Map(normalized, "dependencies");
                var plannedDependencies = JsonData.Map(JsonData.Parse(transaction.manifestJson), "dependencies");
                foreach (ManifestPackageExpectation expected in transaction.expectations.Where(x => x.packageIdentifier is not null && x.installedIdentity.sourceKind == GitPackageSource.sourceKind))
                {
                    string raw = JsonData.Required(normalizedDependencies, expected.identity.id.value);
                    GitPackageSource source = GitPackageSource.ParseDependency(raw, out string reference);
                    if (source.key != expected.installedIdentity.sourceKey || reference != expected.installedIdentity.revision)
                        throw new IOException($"The manifest no longer pins the planned commit for '{expected.identity.id}'.");
                    // UPM may normalize equivalent Git URI spelling. Compare the semantic pin,
                    // and retain the actual spelling in ownership evidence after verification.
                    normalizedDependencies[expected.identity.id.value] = plannedDependencies[expected.identity.id.value];
                }
                if (JsonData.Write(normalized, sortKeys: true) != JsonData.Canonical(transaction.manifestJson))
                    throw new IOException("The project manifest changed during UPM resolution. Ownership was not committed.");
                var declared = JsonData.Map(JsonData.Parse(actual), "dependencies");
                foreach (ManifestPackageExpectation expected in transaction.expectations)
                {
                    PackageInfo? info = infos.SingleOrDefault(x => x.name == expected.identity.id.value);
                    if (info is null || !expected.installedIdentity.Equals(UnityInstalledStateProvider.Identity(info, declared, editor)))
                        throw new IOException($"UPM did not produce the planned identity for '{expected.identity.id}'.");
                }
                foreach (PackageId removed in transaction.removals)
                    if (infos.Any(x => x.name == removed.value)) throw new IOException($"Removed package '{removed}' remains required by effective UPM state.");
                string statePath = UnityProjectFiles.PathFor(project, UnityProjectFiles.ownershipResource);
                if (UnityProjectFiles.Fingerprint(statePath) != transaction.preconditions[UnityProjectFiles.ownershipResource])
                    throw new IOException("Installer ownership state changed during execution. The new receipt was not committed.");
                cancellationToken.ThrowIfCancellationRequested();
                if (transaction.ownershipJson is { } ownership)
                {
                    var committed = UnityProjectFiles.Ownership(ownership);
                    var owners = JsonData.Map(committed, "packages");
                    foreach (ManifestPackageExpectation expected in transaction.expectations.Where(x => x.packageIdentifier is not null))
                        if (owners.TryGetValue(expected.identity.id.value, out object? entry))
                            JsonData.Object(entry)["manifestValue"] = JsonData.Required(declared, expected.identity.id.value);
                    UnityProjectFiles.Write(statePath, JsonData.Write(committed, true) + "\n");
                }
                journal["status"] = "verified";
                journal["afterManifestFingerprint"] = UnityProjectFiles.Fingerprint(manifestPath);
                UnityProjectFiles.Write(journalPath, JsonData.Write(journal, true));
                return new PackageResult<OperationReceipt>(new ManifestExecutionReceipt(journalPath, true, true));
            }
            catch (Exception exception) when (exception is IOException || exception is FormatException || exception is ArgumentException || exception is InvalidOperationException ||
                exception is UnauthorizedAccessException || (exception is OperationCanceledException && applied))
            {
                // Never blindly restore files: native UPM or another actor may have changed them.
                if (journalPath is not null)
                {
                    journal["status"] = applied ? "applied-unverified" : "not-applied";
                    journal["failure"] = exception is OperationCanceledException ? "Cancelled after mutation." : exception.Message;
                    try { UnityProjectFiles.Write(journalPath, JsonData.Write(journal, true)); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
                string detail = applied ? " Manifest may already be applied; inspect the recovery journal before retrying." : "";
                return new PackageResult<OperationReceipt>(journalPath is null ? null : new ManifestExecutionReceipt(journalPath, applied, false), new[]
                {
                    PackageDiagnostic.Error("unity:manifest-execution-failed", PackageDiagnosticPhase.Execution, exception.Message + detail)
                });
            }
            finally
            {
                try
                {
                    if (reloadLocked)
                        await EditorThread.RunAsync(() => { EditorApplication.UnlockReloadAssemblies(); return true; }, CancellationToken.None).ConfigureAwait(false);
                }
                finally { UnityUpmClient.gate.Release(); }
            }
        }
    }
}
