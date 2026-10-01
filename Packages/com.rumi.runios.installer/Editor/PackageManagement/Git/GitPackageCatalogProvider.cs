#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Providers;
using RuniOS.PackageManagement.Resolution;
using System.IO;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using RuniOS.PackageManagement.Unity.Editor.Git.Internal;
using RuniOS.PackageManagement.Unity.Editor.Metadata;
using RuniOS.PackageManagement.Unity.Editor.Internal;
using RuniOS.PackageManagement.Unity.Editor.Internal.Json;

namespace RuniOS.PackageManagement.Unity.Editor.Git
{
    /// <summary>
    /// Resolves repository references and reads metadata from immutable Git objects.<br/>
    /// 저장소 reference를 해석하고 불변 Git object에서 메타데이터를 읽습니다.
    /// </summary>
    public sealed class GitPackageCatalogProvider : IPackageCatalogProvider
    {
        readonly GitRepositoryStore store;
        readonly UpmMetadataReader reader;
        readonly ConditionalWeakTable<ResolutionSession, ConcurrentDictionary<string, Lazy<Task<string>>>> sessions = new();
        /// <inheritdoc/>
        public string sourceKind => GitPackageSource.sourceKind;
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="reader">
        /// The dependency metadata adapter.<br/>
        /// 의존성 메타데이터 어댑터입니다.
        /// </param>
        /// <param name="cacheDirectory">
        /// An optional isolated bare-repository cache directory.<br/>
        /// 선택적인 독립 bare 저장소 cache 디렉터리입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public GitPackageCatalogProvider(UpmMetadataReader reader, string? cacheDirectory = null)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            store = new GitRepositoryStore(cacheDirectory ?? Path.Combine(Path.GetTempPath(), "RuniOS.PackageManagement", "git"));
        }
        /// <inheritdoc/>
        public async Task<PackageResult<CandidateSet>> FindCandidatesAsync(PackageQuery query, CancellationToken cancellationToken)
        {
            if (query.source is not GitPackageSource source)
                return AdapterData.Failure<CandidateSet>("git:invalid-source", PackageDiagnosticPhase.Resolution, "A Git source payload is required.");
            try
            {
                string[] references = query.requirements.SelectMany(x => x.constraints).SelectMany(x =>
                    x is GitReferenceConstraint git ? new[] { git.reference } : x is ExactRevisionConstraint exact ? new[] { exact.revision } : Array.Empty<string>())
                    .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                if (references.Length == 0) references = new[] { "HEAD" };
                var cache = sessions.GetValue(query.session, _ => new());
                Dictionary<string, List<string>> commits = new(StringComparer.Ordinal);
                foreach (string reference in references)
                {
                    string commit = await cache.GetOrAdd(source.repositoryUrl + "\n" + reference,
                        _ => new Lazy<Task<string>>(() => store.ResolveAsync(source.repositoryUrl, reference, cancellationToken))).Value.ConfigureAwait(false);
                    if (!commits.TryGetValue(commit, out List<string>? aliases) || aliases is null) commits.Add(commit, aliases = new());
                    aliases.Add(reference);
                }
                List<PackageCandidate> candidates = new();
                foreach (var entry in commits.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    GitCommitArtifact artifact = new(source, entry.Key, entry.Value);
                    string packageJson = await store.ReadAsync(artifact, "package.json", cancellationToken).ConfigureAwait(false) ??
                        throw new IOException($"package.json was not found at '{source.packagePath}'.");
                    string version = JsonData.Required(JsonData.Parse(packageJson), "version");
                    candidates.Add(new PackageCandidate(new PackageIdentity(query.id, sourceKind, source.key, entry.Key, version), new[] { artifact }));
                }
                return new PackageResult<CandidateSet>(new CandidateSet(candidates));
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is FormatException || exception is System.ComponentModel.Win32Exception)
            {
                return AdapterData.Failure<CandidateSet>("git:discovery-failed", PackageDiagnosticPhase.Resolution, exception.Message);
            }
        }
        /// <inheritdoc/>
        public async Task<PackageResult<PackageMetadata>> GetMetadataAsync(PackageCandidate candidate, CancellationToken cancellationToken)
        {
            try
            {
                GitCommitArtifact artifact = candidate.artifacts.OfType<GitCommitArtifact>().Single();
                if (candidate.identity.sourceKind != sourceKind || candidate.identity.sourceKey != artifact.source.key || candidate.identity.revision != artifact.commit)
                    throw new FormatException("The Git artifact differs from the exact candidate identity.");
                string package = await store.ReadAsync(artifact, "package.json", cancellationToken).ConfigureAwait(false) ?? throw new IOException("package.json is missing.");
                string? sidecar = await store.ReadAsync(artifact, "runios.package.json", cancellationToken).ConfigureAwait(false);
                return reader.Read(candidate, package, sidecar);
            }
            catch (Exception exception) when (exception is IOException || exception is FormatException || exception is InvalidOperationException)
            {
                return AdapterData.Failure<PackageMetadata>("git:metadata-failed", PackageDiagnosticPhase.Resolution, exception.Message);
            }
        }
    }
}
