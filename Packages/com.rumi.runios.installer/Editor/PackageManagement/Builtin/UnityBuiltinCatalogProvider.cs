#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Providers;
using RuniOS.PackageManagement.Resolution;
using System.Collections.Concurrent;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using RuniOS.PackageManagement.Unity.Editor.Internal;
using RuniOS.PackageManagement.Unity.Editor.Metadata;

namespace RuniOS.PackageManagement.Unity.Editor.Builtin
{
    internal sealed class UnityBuiltinCatalogProvider : IPackageCatalogProvider
    {
        readonly UpmSourceConfiguration sources;
        readonly ConcurrentDictionary<PackageIdentity, PackageMetadata> metadata = new();
        public string sourceKind => UnityBuiltinSource.sourceKind;
        internal UnityBuiltinCatalogProvider(UpmSourceConfiguration sources) => this.sources = sources;
        public async Task<PackageResult<CandidateSet>> FindCandidatesAsync(PackageQuery query, CancellationToken cancellationToken)
        {
            if (query.source is not UnityBuiltinSource || query.source.key != query.id.value)
                return AdapterData.Failure<CandidateSet>("unity:invalid-builtin-source", PackageDiagnosticPhase.Resolution, "A built-in source payload is required.");
            await UnityUpmClient.gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                SearchRequest request = await EditorThread.RunAsync(() => Client.Search(query.id.value, true), cancellationToken).ConfigureAwait(false);
                PackageInfo[] infos = await UnityUpmClient.WaitAsync(request, cancellationToken).ConfigureAwait(false);
                PackageInfo? info = infos.SingleOrDefault(x => x.name == query.id.value && x.source == UnityEditor.PackageManager.PackageSource.BuiltIn);
                if (info is null) return AdapterData.Failure<CandidateSet>("unity:builtin-not-found", PackageDiagnosticPhase.Resolution, $"The current Editor has no built-in package '{query.id}'.");
                string editor = await EditorThread.RunAsync(() => Application.unityVersion, cancellationToken).ConfigureAwait(false);
                PackageIdentity identity = new(query.id, sourceKind, query.source.key, editor + ":" + info.version, info.version);
                metadata[identity] = new PackageMetadata(query.id, info.dependencies.Select(x => new PackageDependency(sources.VersionRequirement(new PackageId(x.name), x.version))), info.displayName, info.version);
                return new PackageResult<CandidateSet>(new CandidateSet(new[] { new PackageCandidate(identity, new[] { new UnityBuiltinArtifact(query.id, info.version, editor) }) }));
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException)
            {
                return AdapterData.Failure<CandidateSet>("unity:builtin-discovery-failed", PackageDiagnosticPhase.Resolution, exception.Message);
            }
            finally { UnityUpmClient.gate.Release(); }
        }
        public Task<PackageResult<PackageMetadata>> GetMetadataAsync(PackageCandidate candidate, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(metadata.TryGetValue(candidate.identity, out PackageMetadata? value) && value is not null ?
                new PackageResult<PackageMetadata>(value) : AdapterData.Failure<PackageMetadata>("unity:builtin-metadata-missing", PackageDiagnosticPhase.Resolution, "Built-in metadata was not discovered for this candidate."));
        }
    }
}
