#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Planning;
using RuniOS.PackageManagement.Providers;
using RuniOS.PackageManagement.Unity.Editor.Internal;
using RuniOS.PackageManagement.Unity.Editor.Internal.Json;
using RuniOS.PackageManagement.Unity.Editor.State;
using RuniOS.PackageManagement.Unity.Editor.State.Internal;
using RuniOS.PackageManagement.Unity.Editor.Registry;
using RuniOS.PackageManagement.Unity.Editor.Metadata;

namespace RuniOS.PackageManagement.Unity.Editor.Manifest
{
    /// <summary>
    /// Composes Git, registry and built-in artifacts into one passive manifest transaction.<br/>
    /// Git, registry 및 내장 artifact를 하나의 수동적인 manifest 전이로 합성합니다.
    /// </summary>
    public sealed class UnityManifestPlanningProvider : IInstallationPlanningProvider
    {
        /// <inheritdoc/>
        public string installationKind => UnityManifestInstallation.installationKind;
        /// <inheritdoc/>
        public PackageResult<PlanFragment> Plan(InstallationPlanningContext context)
        {
            try
            {
                if (context.request.current.target is not UnityProjectTarget)
                    throw new ArgumentException("A Unity project target is required.");
                UnityProjectSnapshotFact snapshot = context.request.current.facts.OfType<UnityProjectSnapshotFact>().SingleOrDefault() ??
                    throw new ArgumentException("A Unity installed-state snapshot is required.");
                var document = JsonData.Parse(snapshot.manifestJson);
                var dependencies = JsonData.Map(document, "dependencies"); document["dependencies"] = dependencies;
                var ownership = UnityProjectFiles.Ownership(snapshot.ownershipJson);
                var owners = JsonData.Map(ownership, "packages"); ownership["packages"] = owners;
                bool ownershipChanged = false;
                List<ScopedRegistryDefinition> needed = new();
                List<ManifestPackageExpectation> expectations = new();
                List<PackageOperationBoundary> boundaries = new();
                List<PackageId> removed = new();
                const string transactionId = "unity:manifest-transaction";
                foreach (PackageChange change in context.removeChanges)
                {
                    // Promotion from an indirect dependency has no direct manifest entry to remove.
                    if (change.current?.installation is UnityManifestInstallation { isDirect: true }) dependencies.Remove(change.id.value);
                    if (change.desiredInstallation?.kind != installationKind) removed.Add(change.id);
                    if (owners.Remove(change.id.value)) ownershipChanged = true;
                    boundaries.Add(new PackageOperationBoundary(change.id, PackageOperationPhase.Remove, new[] { transactionId }, new[] { transactionId }));
                }
                foreach (var entry in context.request.desired.packages.OrderBy(x => x.Key.value, StringComparer.Ordinal))
                {
                    if (!context.request.installations.TryGetValue(entry.Key, out PackageInstallation? installation) || installation?.kind != installationKind) continue;
                    if (installation is not UnityManifestInstallation { isDirect: true }) throw new ArgumentException("Desired packages require a direct Unity manifest installation.");
                    IUnityManifestArtifact[] representations = entry.Value.artifacts.OfType<IUnityManifestArtifact>().ToArray();
                    if (representations.Length != 1) throw new ArgumentException($"'{entry.Key}' must provide exactly one Unity manifest artifact representation.");
                    IUnityManifestArtifact artifact = representations[0];
                    AdapterData.Text(artifact.dependencyValue, nameof(artifact.dependencyValue));
                    AdapterData.Text(artifact.packageIdentifier, nameof(artifact.packageIdentifier));
                    dependencies[entry.Key.value] = artifact.dependencyValue;
                    if (artifact.registry is { } registry) needed.Add(registry);
                    PackageIdentity installedIdentity = artifact.GetInstalledIdentity(entry.Key);
                    if (installedIdentity.id != entry.Key) throw new ArgumentException("The artifact returned a different installed package ID.");
                    expectations.Add(new ManifestPackageExpectation(entry.Value.identity, artifact.packageIdentifier, installedIdentity));
                    PackageChange? change = context.installChanges.SingleOrDefault(x => x.id == entry.Key);
                    if (change is not null)
                    {
                        owners[entry.Key.value] = new Dictionary<string, object?>(StringComparer.Ordinal)
                        {
                            ["owner"] = context.request.managementOwner, ["sourceKind"] = entry.Value.identity.sourceKind,
                            ["sourceKey"] = entry.Value.identity.sourceKey, ["revision"] = entry.Value.identity.revision,
                            ["manifestValue"] = artifact.dependencyValue,
                            ["installedSourceKind"] = installedIdentity.sourceKind, ["installedSourceKey"] = installedIdentity.sourceKey,
                            ["installedRevision"] = installedIdentity.revision
                        };
                        ownershipChanged = true;
                        boundaries.Add(new PackageOperationBoundary(entry.Key, PackageOperationPhase.Install, new[] { transactionId }, new[] { transactionId }));
                    }
                }
                MergeRegistries(document, needed);
                UpmSourceConfiguration sources = UnityProjectFiles.Sources(document);
                foreach (ManifestPackageExpectation expected in expectations.Where(x => x.installedIdentity.sourceKind == RegistryPackageSource.sourceKind))
                    if (sources.RegistrySource(expected.identity.id).key != expected.installedIdentity.sourceKey)
                        throw new ArgumentException($"Manifest registry routing would install '{expected.identity.id}' from a different source.");
                foreach (InstalledPackage retained in context.request.current.packages.packages.Values.Where(x => !context.request.desired.packages.ContainsKey(x.id) && !removed.Contains(x.id)))
                {
                    if (retained.identity is not { } identity) continue;
                    PackageIdentity installedIdentity = snapshot.installedIdentities.TryGetValue(retained.id, out PackageIdentity? recorded) && recorded is not null ? recorded : identity;
                    if (installedIdentity.sourceKind == RegistryPackageSource.sourceKind && sources.RegistrySource(retained.id).key != installedIdentity.sourceKey)
                        throw new ArgumentException($"Adding registry scopes would redirect retained package '{retained.id}'.");
                    expectations.Add(new ManifestPackageExpectation(identity, installedIdentity: installedIdentity));
                }
                string manifestJson = JsonData.Write(document, true) + "\n";
                string? ownershipJson = ownershipChanged ? JsonData.Write(ownership, true) + "\n" : snapshot.ownershipJson;
                bool manifestChanged = JsonData.Canonical(snapshot.manifestJson) != JsonData.Write(document, sortKeys: true);
                if (!manifestChanged && !ownershipChanged && boundaries.Count == 0)
                    return new PackageResult<PlanFragment>(new PlanFragment(Array.Empty<PlannedOperation>(), Array.Empty<PackageOperationBoundary>()));
                // A changed registry may require resolution even when the generic package diff is empty.
                if (expectations.All(x => x.packageIdentifier is null) && removed.Count == 0)
                    throw new ArgumentException("The transaction has no UPM package to resolve.");
                foreach (string resource in new[] { UnityProjectFiles.manifestResource, UnityProjectFiles.lockResource, UnityProjectFiles.ownershipResource })
                    if (!context.request.current.resourceFingerprints.ContainsKey(resource)) throw new ArgumentException("The snapshot has incomplete manifest transaction preconditions.");
                UnityManifestOperation operation = new(manifestJson, ownershipJson, context.request.current.resourceFingerprints, expectations, removed);
                PlannedOperation planned = new(transactionId, operation, claims: new[]
                {
                    new ResourceClaim(UnityProjectFiles.manifestResource, ResourceAccess.Write),
                    new ResourceClaim(UnityProjectFiles.lockResource, ResourceAccess.Write),
                    new ResourceClaim(UnityProjectFiles.ownershipResource, ResourceAccess.Write)
                });
                return new PackageResult<PlanFragment>(new PlanFragment(new[] { planned }, boundaries));
            }
            catch (Exception exception) when (exception is ArgumentException || exception is FormatException || exception is InvalidOperationException)
            {
                return AdapterData.Failure<PlanFragment>("unity:manifest-planning-failed", PackageDiagnosticPhase.Planning, exception.Message);
            }
        }
        static void MergeRegistries(Dictionary<string, object?> document, IEnumerable<ScopedRegistryDefinition> desired)
        {
            List<object?> entries = document.TryGetValue("scopedRegistries", out object? value) ? JsonData.Array(value) : new();
            foreach (ScopedRegistryDefinition registry in desired.OrderBy(x => x.url, StringComparer.Ordinal).ThenBy(x => x.name, StringComparer.Ordinal))
            {
                var maps = entries.Select(JsonData.Object).ToArray();
                if (maps.Any(x => JsonData.Required(x, "name") == registry.name && AdapterData.HttpUrl(JsonData.Required(x, "url")) != registry.url))
                    throw new ArgumentException($"Scoped registry name '{registry.name}' already belongs to a different URL.");
                Dictionary<string, object?>? existing = maps.SingleOrDefault(x => AdapterData.HttpUrl(JsonData.Required(x, "url")) == registry.url);
                if (existing is null)
                {
                    existing = new(StringComparer.Ordinal) { ["name"] = registry.name, ["url"] = registry.url, ["scopes"] = new List<object?>() };
                    entries.Add(existing);
                }
                List<object?> scopes = JsonData.Array(existing.TryGetValue("scopes", out object? configured) ? configured : null);
                foreach (string scope in registry.scopes)
                    if (!scopes.Any(x => JsonData.String(x) == scope)) scopes.Add(scope);
                existing["scopes"] = scopes;
            }
            if (entries.Count > 0) document["scopedRegistries"] = entries;
            var config = UnityProjectFiles.Sources(document);
            foreach (string id in JsonData.Map(document, "dependencies").Keys)
                config.RegistrySource(new PackageId(id)); // Reject ambiguous equal-specificity routes.
        }
    }
}
