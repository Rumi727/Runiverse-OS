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
using System.Net.Http;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using RuniOS.PackageManagement.Unity.Editor.Metadata;
using RuniOS.PackageManagement.Unity.Editor.Internal;
using RuniOS.PackageManagement.Unity.Editor.Internal.Json;
using RuniOS.PackageManagement.Unity.Editor.Registry.Internal;

namespace RuniOS.PackageManagement.Unity.Editor.Registry
{
    /// <summary>
    /// Retrieves exact UPM versions and verifies published tarball metadata before resolution.<br/>
    /// 해석 전에 정확한 UPM 버전을 조회하고 배포된 tarball 메타데이터를 검증합니다.
    /// </summary>
    public sealed class RegistryPackageCatalogProvider : IPackageCatalogProvider
    {
        readonly HttpClient client;
        readonly UpmMetadataReader reader;
        readonly ConditionalWeakTable<ResolutionSession, ConcurrentDictionary<string, Lazy<Task<string>>>> sessions = new();
        /// <inheritdoc/>
        public string sourceKind => RegistryPackageSource.sourceKind;
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="reader">
        /// The dependency metadata adapter.<br/>
        /// 의존성 메타데이터 어댑터입니다.
        /// </param>
        /// <param name="client">
        /// The caller-owned HTTP client; scope authentication handlers to their intended origins.<br/>
        /// 호출자가 소유하는 HTTP client이며 인증 handler를 대상 origin으로 제한합니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public RegistryPackageCatalogProvider(UpmMetadataReader reader, HttpClient client)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }
        async Task<byte[]> DownloadAsync(string url, int limit, CancellationToken cancellationToken)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            CancellationToken downloadToken = timeout.Token;
            using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, downloadToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            HttpContent content = response.Content ?? throw new IOException("The registry response has no content.");
            if (content.Headers.ContentLength > limit) throw new IOException("The registry response exceeds the retrieval limit.");
            using Stream input = await content.ReadAsStreamAsync().ConfigureAwait(false);
            using MemoryStream output = new(); byte[] buffer = new byte[8192];
            while (true)
            {
                int count = await input.ReadAsync(buffer, 0, buffer.Length, downloadToken).ConfigureAwait(false);
                if (count == 0) break;
                if (output.Length + count > limit) throw new IOException("The registry response exceeds the retrieval limit.");
                output.Write(buffer, 0, count);
            }
            return output.ToArray();
        }
        /// <inheritdoc/>
        public async Task<PackageResult<CandidateSet>> FindCandidatesAsync(PackageQuery query, CancellationToken cancellationToken)
        {
            if (query.source is not RegistryPackageSource source || source.packageId != query.id)
                return AdapterData.Failure<CandidateSet>("registry:invalid-source", PackageDiagnosticPhase.Resolution, "A matching registry package source is required.");
            try
            {
                string[] requested = query.requirements.SelectMany(x => x.constraints).SelectMany(x => x is UpmVersionConstraint version ? new[] { version.version } :
                    x is ExactRevisionConstraint exact ? new[] { new UpmVersionConstraint(exact.revision).version } : Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray();
                if (requested.Length == 0)
                    return AdapterData.Failure<CandidateSet>("registry:exact-version-required", PackageDiagnosticPhase.Resolution, "Specify an exact UPM version; automatic range and tag selection is not implemented.");
                if (requested.Length > 1) return new PackageResult<CandidateSet>(new CandidateSet(Array.Empty<PackageCandidate>()));
                var cache = sessions.GetValue(query.session, _ => new());
                string document = await cache.GetOrAdd(source.key, _ => new Lazy<Task<string>>(async () =>
                    new System.Text.UTF8Encoding(false, true).GetString(await DownloadAsync(source.registryUrl + "/" + Uri.EscapeDataString(query.id.value), 32 * 1024 * 1024, cancellationToken).ConfigureAwait(false))))
                    .Value.ConfigureAwait(false);
                Dictionary<string, object?> versions = JsonData.Map(JsonData.Parse(document), "versions");
                if (!versions.TryGetValue(requested[0], out object? payload)) return new PackageResult<CandidateSet>(new CandidateSet(Array.Empty<PackageCandidate>()));
                Dictionary<string, object?> version = JsonData.Object(payload);
                if (JsonData.Required(version, "name") != query.id.value || JsonData.Required(version, "version") != requested[0])
                    throw new FormatException("Registry metadata does not match the requested identity.");
                Dictionary<string, object?> distribution = JsonData.Map(version, "dist");
                string? integrity = JsonData.Optional(distribution, "integrity");
                if (integrity is null && JsonData.Optional(distribution, "shasum") is { } shasum)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(shasum, "^[0-9a-fA-F]{40}$")) throw new FormatException("Invalid registry SHA-1 checksum.");
                    byte[] hash = Enumerable.Range(0, 20).Select(i => Convert.ToByte(shasum.Substring(i * 2, 2), 16)).ToArray();
                    integrity = "sha1-" + Convert.ToBase64String(hash);
                }
                if (integrity is null) throw new FormatException("Registry tarball integrity metadata is required.");
                string tarball = JsonData.Required(distribution, "tarball");
                Uri uri = new(tarball, UriKind.Absolute);
                if ((uri.Scheme != "http" && uri.Scheme != "https") || !string.IsNullOrEmpty(uri.UserInfo)) throw new FormatException("A credential-free HTTP(S) tarball URL is required.");
                RegistryPackageArtifact artifact = new(source, requested[0], integrity, tarball);
                return new PackageResult<CandidateSet>(new CandidateSet(new[]
                {
                    new PackageCandidate(new PackageIdentity(query.id, sourceKind, source.key, requested[0], requested[0]), new[] { artifact })
                }));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return AdapterData.Failure<CandidateSet>("registry:request-timeout", PackageDiagnosticPhase.Resolution, "The registry request timed out.");
            }
            catch (Exception exception) when (exception is HttpRequestException || exception is IOException || exception is FormatException || exception is ArgumentException)
            {
                return AdapterData.Failure<CandidateSet>("registry:discovery-failed", PackageDiagnosticPhase.Resolution, exception.Message);
            }
        }
        /// <inheritdoc/>
        public async Task<PackageResult<PackageMetadata>> GetMetadataAsync(PackageCandidate candidate, CancellationToken cancellationToken)
        {
            try
            {
                RegistryPackageArtifact artifact = candidate.artifacts.OfType<RegistryPackageArtifact>().Single();
                if (candidate.identity.sourceKind != sourceKind || candidate.identity.sourceKey != artifact.source.key || candidate.identity.revision != artifact.version)
                    throw new FormatException("The registry artifact differs from the exact candidate identity.");
                byte[] bytes = await DownloadAsync(artifact.tarballUrl ?? throw new FormatException("A tarball URL is required."), 128 * 1024 * 1024, cancellationToken).ConfigureAwait(false);
                RegistryTarball.Verify(bytes, artifact.integrity ?? throw new FormatException("Tarball integrity metadata is required."));
                (string package, string? sidecar) = await Task.Run(() => RegistryTarball.Read(bytes, cancellationToken), cancellationToken).ConfigureAwait(false);
                return reader.Read(candidate, package, sidecar);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return AdapterData.Failure<PackageMetadata>("registry:request-timeout", PackageDiagnosticPhase.Resolution, "The registry request timed out.");
            }
            catch (Exception exception) when (exception is HttpRequestException || exception is IOException || exception is FormatException || exception is ArgumentException || exception is InvalidOperationException || exception is OverflowException)
            {
                return AdapterData.Failure<PackageMetadata>("registry:metadata-failed", PackageDiagnosticPhase.Resolution, exception.Message);
            }
        }
    }
}
