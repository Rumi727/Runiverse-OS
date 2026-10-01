#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Searches candidate combinations and publishes only complete, compatible, acyclic graphs.<br/>
    /// 후보 조합을 탐색하고 완전하고 호환되며 순환 없는 그래프만 제공합니다.
    /// </summary>
    public sealed class PackageResolver : IPackageResolver
    {
        readonly PackageManagementServices services;

        internal PackageResolver(PackageManagementServices services) => this.services = services;

        /// <summary>
        /// Resolves the complete dependency closure, revisiting candidates when later requirements conflict.<br/>
        /// 나중에 추가된 요구가 충돌하면 후보를 다시 탐색하여 전체 의존성 집합을 해석합니다.
        /// </summary>
        /// <param name="request">
        /// All retained root requirements, source overrides, and the search budget.<br/>
        /// 유지할 전체 루트 요구, 출처 재지정 및 탐색 예산입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns a validated graph or diagnostics without a graph.<br/>
        /// 완료되면 검증된 그래프를 반환하거나 그래프 없이 진단을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="request"/> is <see langword="null"/>.<br/>
        /// <paramref name="request"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when cancellation is requested.<br/>
        /// 취소가 요청되면 발생합니다.
        /// </exception>
        public Task<PackageResult<ResolvedPackageGraph>> ResolveAsync(ResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            return new Search(services, request, cancellationToken).RunAsync(new Dictionary<PackageId, Selection>());
        }

        sealed class Selection
        {
            internal PackageCandidate Candidate { get; }
            internal PackageMetadata Metadata { get; }
            internal IReadOnlyList<PackageDiagnostic> Diagnostics { get; }

            internal Selection(PackageCandidate candidate, PackageMetadata metadata, IEnumerable<PackageDiagnostic> diagnostics)
            {
                Candidate = candidate;
                Metadata = metadata;
                Diagnostics = Snapshots.List(diagnostics);
            }
        }

        sealed class Search
        {
            readonly PackageManagementServices services;
            readonly ResolutionRequest request;
            readonly CancellationToken cancellationToken;
            readonly Dictionary<PackageIdentity, PackageResult<PackageMetadata>> metadataCache = new();
            readonly ResolutionSession session = new();
            int attempts;

            internal Search(PackageManagementServices services, ResolutionRequest request, CancellationToken cancellationToken)
            {
                this.services = services;
                this.request = request;
                this.cancellationToken = cancellationToken;
            }

            internal async Task<PackageResult<ResolvedPackageGraph>> RunAsync(Dictionary<PackageId, Selection> selected)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Dictionary<PackageId, List<PackageRequirement>> requirements = request.Roots
                    .Concat(selected.Values.SelectMany(x => x.Metadata.Dependencies.Select(y => y.Requirement)))
                    .GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.ToList());
                Dictionary<PackageId, PackageSource> sources = new();
                List<PackageDiagnostic> diagnostics = new();

                foreach (KeyValuePair<PackageId, List<PackageRequirement>> group in requirements)
                {
                    PackageSource? source = null;
                    if (request.SourceBindings.TryGetValue(group.Key, out PackageSource? bound))
                        source = bound;
                    else
                    {
                        foreach (PackageSource hint in group.Value.Select(x => x.SourceHint).OfType<PackageSource>())
                        {
                            if (source is not null && !Snapshots.SameSource(source, hint))
                                diagnostics.Add(Error("conflicting-sources", $"Requirements for '{group.Key}' specify different sources.", group.Key, group.Value));
                            source = hint;
                        }
                    }
                    if (source is not null)
                        sources.Add(group.Key, source);
                    foreach (PackageConstraint constraint in group.Value.SelectMany(x => x.Constraints))
                    {
                        if (!services.Evaluators.ContainsKey(constraint.Kind))
                            diagnostics.Add(Error("unsupported-constraint", $"No evaluator handles '{constraint.Kind}'.", group.Key, group.Value));
                    }
                }
                if (diagnostics.Count > 0)
                    return Failure(diagnostics);

                foreach (KeyValuePair<PackageId, Selection> selection in selected)
                {
                    if (!sources.TryGetValue(selection.Key, out PackageSource? source) || source is null ||
                        !MatchesSource(selection.Value.Candidate, source) ||
                        !Matches(selection.Value.Candidate, requirements[selection.Key], diagnostics))
                    {
                        diagnostics.Add(Error("conflicting-requirements", $"Selected revision for '{selection.Key}' no longer satisfies all requirements.",
                            selection.Key, requirements[selection.Key]));
                        return Failure(diagnostics);
                    }
                }

                if (!GraphOrder.TryOrder(selected.Keys.OrderBy(x => x.Value, StringComparer.Ordinal),
                    id => selected.TryGetValue(id, out Selection? selection) && selection is not null
                        ? selection.Metadata.Dependencies.Select(x => x.Requirement.Id)
                        : Array.Empty<PackageId>(), out _, out IReadOnlyList<PackageId> cycle))
                    return Failure(new[] { PackageDiagnostic.Error("package:dependency-cycle", PackageDiagnosticPhase.Resolution,
                        "Selected dependencies contain a cycle.", cycle,
                        cycle.Where(requirements.ContainsKey).SelectMany(id => requirements[id]).Select(x => x.Location).OfType<DeclarationLocation>()) });

                PackageId[] unresolved = requirements.Keys.Where(x => !selected.ContainsKey(x))
                    .OrderBy(x => x.Value, StringComparer.Ordinal).ToArray();
                if (unresolved.Length == 0)
                {
                    ResolvedPackageGraph graph = new(selected.Values.Select(x => new ResolvedPackage(x.Candidate, x.Metadata)), request.Roots);
                    return new PackageResult<ResolvedPackageGraph>(graph, selected.Values.SelectMany(x => x.Diagnostics).Concat(diagnostics));
                }

                // A dependency may obtain a source hint from metadata of another pending root.
                PackageId[] available = unresolved.Where(sources.ContainsKey).ToArray();
                if (available.Length == 0)
                    return Failure(unresolved.Select(id => Error("missing-source", $"No source is bound to '{id}'.", id, requirements[id])));
                PackageId next = available[0];
                PackageSource nextSource = sources[next];
                if (!services.Catalogs.TryGetValue(nextSource.Kind, out IPackageCatalogProvider? catalog) || catalog is null)
                    return Failure(new[] { Error("unsupported-source", $"No catalog handles '{nextSource.Kind}'.", next, requirements[next]) });

                PackageResult<CandidateSet> result = await catalog.FindCandidatesAsync(
                    new PackageQuery(next, nextSource, requirements[next], session), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                CandidateSet? candidates = result.Value;
                if (!result.Succeeded || candidates is null)
                    return ProviderFailure(result.Diagnostics, next, requirements[next]);
                if (!candidates.IsComplete)
                    return Failure(result.Diagnostics.Concat(new[] { Error("incomplete-candidate-search",
                        $"The catalog did not complete the candidate search for '{next}'.", next, requirements[next]) }));

                HashSet<PackageIdentity> identities = new();
                foreach (PackageCandidate candidate in candidates.Candidates)
                {
                    if (candidate.Identity.Id != next || !MatchesSource(candidate, nextSource))
                        diagnostics.Add(Error("invalid-candidate", $"A catalog candidate does not belong to the query for '{next}'.", next, requirements[next]));
                    if (!identities.Add(candidate.Identity))
                        diagnostics.Add(Error("duplicate-candidate", $"The catalog returned a duplicate exact identity for '{next}'.", next, requirements[next]));
                }
                if (diagnostics.Any(x => x.Severity == PackageDiagnosticSeverity.Error))
                    return Failure(result.Diagnostics.Concat(diagnostics));

                IReadOnlyList<PackageDiagnostic>? lastFailure = null;
                foreach (PackageCandidate candidate in candidates.Candidates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (++attempts > request.MaximumCandidateAttempts)
                        return Failure(new[] { Error("resolution-limit", "The candidate search budget was exhausted.", next, requirements[next]) });
                    List<PackageDiagnostic> candidateDiagnostics = new(result.Diagnostics);
                    if (!Matches(candidate, requirements[next], candidateDiagnostics))
                    {
                        if (candidateDiagnostics.Any(x => x.Severity == PackageDiagnosticSeverity.Error))
                            lastFailure = candidateDiagnostics;
                        continue;
                    }
                    if (!metadataCache.TryGetValue(candidate.Identity, out PackageResult<PackageMetadata>? metadataResult) || metadataResult is null)
                    {
                        metadataResult = await catalog.ReadMetadataAsync(candidate, cancellationToken).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                        metadataCache.Add(candidate.Identity, metadataResult);
                    }
                    PackageMetadata? metadata = metadataResult.Value;
                    if (!metadataResult.Succeeded || metadata is null)
                    {
                        lastFailure = ProviderFailure(metadataResult.Diagnostics, next, requirements[next]).Diagnostics;
                        continue;
                    }
                    if (metadata.Id != next)
                    {
                        lastFailure = new[] { Error("metadata-id-mismatch", $"Metadata does not identify '{next}'.", next, requirements[next]) };
                        continue;
                    }
                    if (candidate.Identity.Version is { } candidateVersion && metadata.Version is { } metadataVersion && candidateVersion != metadataVersion)
                    {
                        lastFailure = new[] { Error("metadata-version-mismatch", $"Candidate and metadata versions disagree for '{next}'.", next, requirements[next]) };
                        continue;
                    }
                    Dictionary<PackageId, Selection> branch = new(selected)
                    {
                        [next] = new Selection(candidate, metadata, candidateDiagnostics.Concat(metadataResult.Diagnostics))
                    };
                    PackageResult<ResolvedPackageGraph> branchResult = await RunAsync(branch).ConfigureAwait(false);
                    if (branchResult.Succeeded)
                        return branchResult;
                    lastFailure = branchResult.Diagnostics;
                    if (attempts > request.MaximumCandidateAttempts)
                        return branchResult;
                }
                return Failure(result.Diagnostics.Concat(lastFailure ?? Array.Empty<PackageDiagnostic>()).Concat(new[]
                {
                    Error("no-compatible-selection", $"No candidate combination satisfies the requirements for '{next}'.", next, requirements[next])
                }));
            }

            bool Matches(PackageCandidate candidate, IEnumerable<PackageRequirement> requirements, List<PackageDiagnostic> diagnostics)
            {
                bool accepted = true;
                foreach (PackageConstraint constraint in requirements.SelectMany(x => x.Constraints))
                {
                    ConstraintMatch match = services.Evaluators[constraint.Kind].Evaluate(constraint, candidate);
                    diagnostics.AddRange(match.Diagnostics);
                    accepted &= match.IsMatch && !match.Diagnostics.Any(x => x.Severity == PackageDiagnosticSeverity.Error);
                }
                return accepted;
            }

            static bool MatchesSource(PackageCandidate candidate, PackageSource source) =>
                candidate.Identity.SourceKind == source.Kind && candidate.Identity.SourceKey == source.Key;

            static PackageDiagnostic Error(string code, string message, PackageId? id, IEnumerable<PackageRequirement> requirements) =>
                PackageDiagnostic.Error("package:" + code, PackageDiagnosticPhase.Resolution, message,
                    id.HasValue ? new[] { id.Value } : null, requirements.Select(x => x.Location).OfType<DeclarationLocation>());

            static PackageResult<ResolvedPackageGraph> Failure(IEnumerable<PackageDiagnostic> diagnostics) => new(null, diagnostics);

            static PackageResult<ResolvedPackageGraph> ProviderFailure(IEnumerable<PackageDiagnostic> diagnostics, PackageId id,
                IEnumerable<PackageRequirement> requirements)
            {
                List<PackageDiagnostic> copy = new(diagnostics);
                if (!copy.Any(x => x.Severity == PackageDiagnosticSeverity.Error))
                    copy.Add(Error("missing-provider-result", $"The provider returned no usable result for '{id}'.", id, requirements));
                return Failure(copy);
            }
        }
    }
}
