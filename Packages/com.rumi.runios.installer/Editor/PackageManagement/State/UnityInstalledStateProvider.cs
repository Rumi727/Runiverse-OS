#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Providers;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityPackageSource = UnityEditor.PackageManager.PackageSource;
using RuniOS.PackageManagement.Unity.Editor.Internal;
using RuniOS.PackageManagement.Unity.Editor.Internal.Json;
using RuniOS.PackageManagement.Unity.Editor.State.Internal;
using RuniOS.PackageManagement.Unity.Editor.Git;
using RuniOS.PackageManagement.Unity.Editor.Registry;
using RuniOS.PackageManagement.Unity.Editor.Builtin;
using RuniOS.PackageManagement.Unity.Editor.Metadata;
using RuniOS.PackageManagement.Unity.Editor.Manifest;

namespace RuniOS.PackageManagement.Unity.Editor.State
{
    /// <summary>
    /// Observes effective UPM packages and exact manifest, lock and ownership fingerprints.<br/>
    /// 실제 UPM 패키지와 manifest, lock 및 소유권의 정확한 지문을 관측합니다.
    /// </summary>
    public sealed class UnityInstalledStateProvider : IInstalledStateProvider
    {
        readonly IReadOnlyList<IUpmDependencyDeclarationReader>? readers;
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="readers">
        /// Optional complete declaration-reader registrations; <see langword="null"/> uses the built-in readers.<br/>
        /// 선택적인 전체 선언 리더 등록이며 <see langword="null"/>이면 기본 리더를 사용합니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public UnityInstalledStateProvider(IEnumerable<IUpmDependencyDeclarationReader>? readers = null) => this.readers = readers is null ? null : AdapterData.List(readers);
        /// <inheritdoc/>
        public string targetKind => UnityProjectTarget.targetKind;
        /// <inheritdoc/>
        public async Task<PackageResult<InstalledEnvironmentSnapshot>> ReadAsync(InstallationTarget target, CancellationToken cancellationToken)
        {
            if (target is not UnityProjectTarget project)
                return AdapterData.Failure<InstalledEnvironmentSnapshot>("unity:invalid-target", PackageDiagnosticPhase.Observation, "A Unity project target is required.");
            await UnityUpmClient.gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EnsureCurrentAsync(project, cancellationToken).ConfigureAwait(false);
                var before = UnityProjectFiles.Fingerprints(project);
                PackageInfo[] infos = await UnityUpmClient.ListAsync(cancellationToken).ConfigureAwait(false);
                string editor = await EditorThread.RunAsync(() => Application.unityVersion, cancellationToken).ConfigureAwait(false);
                PackageResult<InstalledEnvironmentSnapshot> result = Capture(project, infos, editor, readers);
                UnityProjectFiles.Check(project, before);
                return result;
            }
            catch (Exception exception) when (exception is IOException || exception is FormatException || exception is ArgumentException || exception is InvalidOperationException)
            {
                return AdapterData.Failure<InstalledEnvironmentSnapshot>("unity:observation-failed", PackageDiagnosticPhase.Observation, exception.Message);
            }
            finally { UnityUpmClient.gate.Release(); }
        }
        internal static Task<bool> EnsureCurrentAsync(UnityProjectTarget target, CancellationToken cancellationToken) => EditorThread.RunAsync(() =>
        {
            string current = new UnityProjectTarget(Directory.GetParent(Application.dataPath)?.FullName ?? throw new IOException("The current Unity project directory is unavailable.")).projectPath;
            StringComparison comparison = Environment.OSVersion.Platform == PlatformID.Win32NT ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!string.Equals(current, target.projectPath, comparison)) throw new ArgumentException("UPM observation and execution must target the currently open Unity project.");
            return true;
        }, cancellationToken);
        internal static PackageIdentity? Identity(PackageInfo info, Dictionary<string, object?> dependencies, string editor)
        {
            PackageId id = new(AdapterData.PackageName(info.name));
            if (info.source == UnityPackageSource.Git)
            {
                string value = dependencies.TryGetValue(info.name, out object? declared) ? JsonData.String(declared) : info.packageId.Substring(info.name.Length + 1);
                GitPackageSource source = GitPackageSource.ParseDependency(value, out _);
                string? commit = info.git?.hash;
                if (commit is null || commit.Length == 0) return null;
                new GitCommitArtifact(source, commit);
                return new PackageIdentity(id, GitPackageSource.sourceKind, source.key, commit, info.version);
            }
            if (info.source == UnityPackageSource.Registry && info.registry is { } registry)
            {
                RegistryPackageSource source = new(id, registry.url);
                return new PackageIdentity(id, source.kind, source.key, info.version, info.version);
            }
            if (info.source == UnityPackageSource.BuiltIn)
                return new PackageIdentity(id, UnityBuiltinSource.sourceKind, id.value, editor + ":" + info.version, info.version);
            return null;
        }
        internal static PackageResult<InstalledEnvironmentSnapshot> Capture(UnityProjectTarget target, PackageInfo[] infos, string editor, IEnumerable<IUpmDependencyDeclarationReader>? readers = null)
        {
            var fingerprints = UnityProjectFiles.Fingerprints(target);
            string manifestText = File.ReadAllText(UnityProjectFiles.PathFor(target, UnityProjectFiles.manifestResource));
            string statePath = UnityProjectFiles.PathFor(target, UnityProjectFiles.ownershipResource);
            string? stateText = File.Exists(statePath) ? File.ReadAllText(statePath) : null;
            var manifest = JsonData.Parse(manifestText);
            var declared = JsonData.Map(manifest, "dependencies");
            var ownership = JsonData.Map(UnityProjectFiles.Ownership(stateText), "packages");
            UpmSourceConfiguration sources = UnityProjectFiles.Sources(manifest);
            UpmMetadataReader reader = new(sources, readers);
            string lockPath = UnityProjectFiles.PathFor(target, UnityProjectFiles.lockResource);
            Dictionary<string, object?> locked = File.Exists(lockPath) ? JsonData.Map(JsonData.Parse(File.ReadAllText(lockPath)), "dependencies") : new(StringComparer.Ordinal);
            List<PackageDiagnostic> diagnostics = new();
            List<InstalledPackage> packages = new();
            Dictionary<PackageId, PackageIdentity> installedIdentities = new();
            HashSet<string> observed = new(StringComparer.Ordinal);
            foreach (PackageInfo info in infos.OrderBy(x => x.name, StringComparer.Ordinal))
            {
                observed.Add(info.name);
                PackageId id = new(AdapterData.PackageName(info.name));
                PackageIdentity? identity = Identity(info, declared, editor);
                if (identity is not null) installedIdentities.Add(id, identity);
                bool manifestManaged = info.source == UnityPackageSource.Git || info.source == UnityPackageSource.Registry || info.source == UnityPackageSource.BuiltIn;
                PackageInstallation installation = manifestManaged ? new UnityManifestInstallation(info.isDirectDependency) : new ObservedPackageInstallation(info.resolvedPath);
                string? owner = null;
                if (ownership.TryGetValue(info.name, out object? state))
                {
                    var entry = JsonData.Object(state);
                    bool matches = identity is not null && info.isDirectDependency && declared.TryGetValue(info.name, out object? value) &&
                        JsonData.String(value) == JsonData.Required(entry, "manifestValue") && identity.sourceKind == JsonData.Required(entry, "installedSourceKind") &&
                        identity.sourceKey == JsonData.Required(entry, "installedSourceKey") && identity.revision == JsonData.Required(entry, "installedRevision");
                    if (matches)
                    {
                        owner = JsonData.Required(entry, "owner");
                        identity = new PackageIdentity(id, JsonData.Required(entry, "sourceKind"), JsonData.Required(entry, "sourceKey"), JsonData.Required(entry, "revision"), info.version);
                    }
                    else diagnostics.Add(new PackageDiagnostic("unity:ownership-state-drift", PackageDiagnosticSeverity.Warning, PackageDiagnosticPhase.Observation, $"Ownership evidence for '{id}' no longer matches effective state."));
                }
                if (locked.TryGetValue(info.name, out object? lockValue) && identity is not null)
                {
                    var entry = JsonData.Object(lockValue);
                    string? recorded = info.source == UnityPackageSource.Git ? JsonData.Optional(entry, "hash") : JsonData.Optional(entry, "version");
                    string expected = info.source == UnityPackageSource.Git ? installedIdentities[id].revision : info.version;
                    if (recorded != expected)
                        diagnostics.Add(PackageDiagnostic.Error("unity:installed-lock-mismatch", PackageDiagnosticPhase.Observation, $"Effective '{id}' differs from packages-lock.json."));
                }
                List<PackageRequirement> requirements = new();
                if (identity is not null && File.Exists(Path.Combine(info.resolvedPath, "package.json")))
                {
                    List<PackageArtifactReference> artifacts = new();
                    if (info.source == UnityPackageSource.Git && declared.TryGetValue(info.name, out object? gitValue))
                        artifacts.Add(new GitCommitArtifact(GitPackageSource.ParseDependency(JsonData.String(gitValue), out _), installedIdentities[id].revision));
                    string sidecar = Path.Combine(info.resolvedPath, "runios.package.json");
                    PackageResult<PackageMetadata> result = reader.Read(new PackageCandidate(identity, artifacts), File.ReadAllText(Path.Combine(info.resolvedPath, "package.json")),
                        File.Exists(sidecar) ? File.ReadAllText(sidecar) : null);
                    if (result.succeeded && result.value is { } metadata) requirements.AddRange(metadata.dependencies.Select(x => x.requirement));
                    else diagnostics.Add(new PackageDiagnostic("unity:unknown-installed-requirements", PackageDiagnosticSeverity.Warning, PackageDiagnosticPhase.Observation,
                        $"Dependency requirements of '{id}' are unavailable. Changes used by this retained package will be blocked."));
                }
                packages.Add(new InstalledPackage(id, identity, installation, owner, info.dependencies.Select(x => new PackageId(x.name)), requirements));
            }
            foreach (string id in declared.Keys.Where(x => !observed.Contains(x)))
                diagnostics.Add(PackageDiagnostic.Error("unity:manifest-package-not-installed", PackageDiagnosticPhase.Observation, $"Manifest dependency '{id}' has no effective UPM package."));
            UnityProjectFiles.Check(target, fingerprints);
            PackageResult<InstalledPackageGraph> graph = InstalledPackageGraph.Create(packages);
            diagnostics.AddRange(graph.diagnostics);
            return new PackageResult<InstalledEnvironmentSnapshot>(graph.value is { } installed ?
                new InstalledEnvironmentSnapshot(target, installed, fingerprints, new[] { new UnityProjectSnapshotFact(manifestText, stateText, installedIdentities) }) : null, diagnostics);
        }
    }
}
