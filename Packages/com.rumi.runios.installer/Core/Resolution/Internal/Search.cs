#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Providers;
using RuniOS.PackageManagement.Resolution;

namespace RuniOS.PackageManagement.Resolution.Internal
{
    internal sealed class Search
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
            Dictionary<PackageId, List<PackageRequirement>> requirements = request.roots
                .Concat(selected.Values.SelectMany(x => x.metadata.dependencies.Select(y => y.requirement)))
                .GroupBy(x => x.id).ToDictionary(x => x.Key, x => x.ToList());
            Dictionary<PackageId, PackageSource> sources = new();
            List<PackageDiagnostic> diagnostics = new();

            foreach (KeyValuePair<PackageId, List<PackageRequirement>> group in requirements)
            {
                PackageSource? source = null;
                if (request.sourceBindings.TryGetValue(group.Key, out PackageSource? bound))
                    source = bound;
                else
                {
                    foreach (PackageSource hint in group.Value.Select(x => x.sourceHint).OfType<PackageSource>())
                    {
                        if (source is not null && !Snapshots.SameSource(source, hint))
                            diagnostics.Add(Error("conflicting-sources", $"Requirements for '{group.Key}' specify different sources.", group.Key, group.Value));
                        source = hint;
                    }
                }
                if (source is not null)
                    sources.Add(group.Key, source);
                foreach (PackageConstraint constraint in group.Value.SelectMany(x => x.constraints))
                {
                    if (!services.evaluators.ContainsKey(constraint.kind))
                        diagnostics.Add(Error("unsupported-constraint", $"No evaluator handles '{constraint.kind}'.", group.Key, group.Value));
                }
            }
            if (diagnostics.Count > 0)
                return Failure(diagnostics);

            foreach (KeyValuePair<PackageId, Selection> selection in selected)
            {
                if (!sources.TryGetValue(selection.Key, out PackageSource? source) || source is null ||
                    !MatchesSource(selection.Value.candidate, source) ||
                    !Matches(selection.Value.candidate, requirements[selection.Key], diagnostics))
                {
                    diagnostics.Add(Error("conflicting-requirements", $"Selected revision for '{selection.Key}' no longer satisfies all requirements.",
                        selection.Key, requirements[selection.Key]));
                    return Failure(diagnostics);
                }
            }

            if (!GraphOrder.TryOrder(selected.Keys.OrderBy(x => x.value, StringComparer.Ordinal),
                id => selected.TryGetValue(id, out Selection? selection) && selection is not null
                    ? selection.metadata.dependencies.Select(x => x.requirement.id)
                    : Array.Empty<PackageId>(), out _, out IReadOnlyList<PackageId> cycle))
                return Failure(new[] { PackageDiagnostic.Error("package:dependency-cycle", PackageDiagnosticPhase.Resolution,
                    "Selected dependencies contain a cycle.", cycle,
                    cycle.Where(requirements.ContainsKey).SelectMany(id => requirements[id]).Select(x => x.location).OfType<DeclarationLocation>()) });

            PackageId[] unresolved = requirements.Keys.Where(x => !selected.ContainsKey(x))
                .OrderBy(x => x.value, StringComparer.Ordinal).ToArray();
            if (unresolved.Length == 0)
            {
                ResolvedPackageGraph graph = new(selected.Values.Select(x => new ResolvedPackage(x.candidate, x.metadata)), request.roots);
                return new PackageResult<ResolvedPackageGraph>(graph, selected.Values.SelectMany(x => x.diagnostics).Concat(diagnostics));
            }

            // A dependency may obtain a source hint from metadata of another pending root.
            PackageId[] available = unresolved.Where(sources.ContainsKey).ToArray();
            if (available.Length == 0)
                return Failure(unresolved.Select(id => Error("missing-source", $"No source is bound to '{id}'.", id, requirements[id])));
            PackageId next = available[0];
            PackageSource nextSource = sources[next];
            if (!services.catalogs.TryGetValue(nextSource.kind, out IPackageCatalogProvider? catalog) || catalog is null)
                return Failure(new[] { Error("unsupported-source", $"No catalog handles '{nextSource.kind}'.", next, requirements[next]) });

            PackageResult<CandidateSet> result = await catalog.FindCandidatesAsync(
                new PackageQuery(next, nextSource, requirements[next], session), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            CandidateSet? candidates = result.value;
            if (!result.succeeded || candidates is null)
                return ProviderFailure(result.diagnostics, next, requirements[next]);
            if (!candidates.isComplete)
                return Failure(result.diagnostics.Concat(new[] { Error("incomplete-candidate-search",
                    $"The catalog did not complete the candidate search for '{next}'.", next, requirements[next]) }));

            HashSet<PackageIdentity> identities = new();
            foreach (PackageCandidate candidate in candidates.candidates)
            {
                if (candidate.identity.id != next || !MatchesSource(candidate, nextSource))
                    diagnostics.Add(Error("invalid-candidate", $"A catalog candidate does not belong to the query for '{next}'.", next, requirements[next]));
                if (!identities.Add(candidate.identity))
                    diagnostics.Add(Error("duplicate-candidate", $"The catalog returned a duplicate exact identity for '{next}'.", next, requirements[next]));
            }
            if (diagnostics.Any(x => x.severity == PackageDiagnosticSeverity.Error))
                return Failure(result.diagnostics.Concat(diagnostics));

            IReadOnlyList<PackageDiagnostic>? lastFailure = null;
            foreach (PackageCandidate candidate in candidates.candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (++attempts > request.maximumCandidateAttempts)
                    return Failure(new[] { Error("resolution-limit", "The candidate search budget was exhausted.", next, requirements[next]) });
                List<PackageDiagnostic> candidateDiagnostics = new(result.diagnostics);
                if (!Matches(candidate, requirements[next], candidateDiagnostics))
                {
                    if (candidateDiagnostics.Any(x => x.severity == PackageDiagnosticSeverity.Error))
                        lastFailure = candidateDiagnostics;
                    continue;
                }
                if (!metadataCache.TryGetValue(candidate.identity, out PackageResult<PackageMetadata>? metadataResult) || metadataResult is null)
                {
                    metadataResult = await catalog.ReadMetadataAsync(candidate, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    metadataCache.Add(candidate.identity, metadataResult);
                }
                PackageMetadata? metadata = metadataResult.value;
                if (!metadataResult.succeeded || metadata is null)
                {
                    lastFailure = ProviderFailure(metadataResult.diagnostics, next, requirements[next]).diagnostics;
                    continue;
                }
                if (metadata.id != next)
                {
                    lastFailure = new[] { Error("metadata-id-mismatch", $"Metadata does not identify '{next}'.", next, requirements[next]) };
                    continue;
                }
                if (candidate.identity.version is { } candidateVersion && metadata.version is { } metadataVersion && candidateVersion != metadataVersion)
                {
                    lastFailure = new[] { Error("metadata-version-mismatch", $"Candidate and metadata versions disagree for '{next}'.", next, requirements[next]) };
                    continue;
                }
                Dictionary<PackageId, Selection> branch = new(selected)
                {
                    [next] = new Selection(candidate, metadata, candidateDiagnostics.Concat(metadataResult.diagnostics))
                };
                PackageResult<ResolvedPackageGraph> branchResult = await RunAsync(branch).ConfigureAwait(false);
                if (branchResult.succeeded)
                    return branchResult;
                lastFailure = branchResult.diagnostics;
                if (attempts > request.maximumCandidateAttempts)
                    return branchResult;
            }
            return Failure(result.diagnostics.Concat(lastFailure ?? Array.Empty<PackageDiagnostic>()).Concat(new[]
            {
                Error("no-compatible-selection", $"No candidate combination satisfies the requirements for '{next}'.", next, requirements[next])
            }));
        }

        bool Matches(PackageCandidate candidate, IEnumerable<PackageRequirement> requirements, List<PackageDiagnostic> diagnostics)
        {
            bool accepted = true;
            foreach (PackageConstraint constraint in requirements.SelectMany(x => x.constraints))
            {
                ConstraintMatch match = services.evaluators[constraint.kind].Evaluate(constraint, candidate);
                diagnostics.AddRange(match.diagnostics);
                accepted &= match.isMatch && !match.diagnostics.Any(x => x.severity == PackageDiagnosticSeverity.Error);
            }
            return accepted;
        }

        static bool MatchesSource(PackageCandidate candidate, PackageSource source) =>
            candidate.identity.sourceKind == source.kind && candidate.identity.sourceKey == source.key;

        static PackageDiagnostic Error(string code, string message, PackageId? id, IEnumerable<PackageRequirement> requirements) =>
            PackageDiagnostic.Error("package:" + code, PackageDiagnosticPhase.Resolution, message,
                id.HasValue ? new[] { id.Value } : null, requirements.Select(x => x.location).OfType<DeclarationLocation>());

        static PackageResult<ResolvedPackageGraph> Failure(IEnumerable<PackageDiagnostic> diagnostics) => new(null, diagnostics);

        static PackageResult<ResolvedPackageGraph> ProviderFailure(IEnumerable<PackageDiagnostic> diagnostics, PackageId id,
            IEnumerable<PackageRequirement> requirements)
        {
            List<PackageDiagnostic> copy = new(diagnostics);
            if (!copy.Any(x => x.severity == PackageDiagnosticSeverity.Error))
                copy.Add(Error("missing-provider-result", $"The provider returned no usable result for '{id}'.", id, requirements));
            return Failure(copy);
        }
    }
}
