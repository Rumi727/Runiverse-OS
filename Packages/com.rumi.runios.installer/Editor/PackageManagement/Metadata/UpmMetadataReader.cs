#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Internal;
using RuniOS.PackageManagement.Unity.Editor.Internal.Json;
using RuniOS.PackageManagement.Unity.Editor.Git;
using RuniOS.PackageManagement.Unity.Editor.Registry;
using RuniOS.PackageManagement.Unity.Editor.Manifest;

namespace RuniOS.PackageManagement.Unity.Editor.Metadata
{
    /// <summary>
    /// Adapts package.json and optional runios.package.json into dependency declarations.<br/>
    /// package.json과 선택적인 runios.package.json을 의존성 선언으로 변환합니다.
    /// </summary>
    public sealed class UpmMetadataReader
    {
        readonly UpmSourceConfiguration sources;
        readonly IReadOnlyDictionary<string, IUpmDependencyDeclarationReader> readers;
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="sources">
        /// The immutable registry routing configuration.<br/>
        /// 불변 레지스트리 연결 설정입니다.
        /// </param>
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
        public UpmMetadataReader(UpmSourceConfiguration sources, IEnumerable<IUpmDependencyDeclarationReader>? readers = null)
        {
            this.sources = sources ?? throw new ArgumentNullException(nameof(sources));
            Dictionary<string, IUpmDependencyDeclarationReader> registrations = new(StringComparer.Ordinal);
            foreach (IUpmDependencyDeclarationReader reader in readers ?? new IUpmDependencyDeclarationReader[] { new GitDependencyDeclarationReader(), new RegistryDependencyDeclarationReader() })
                registrations.Add(reader.sourceKind, reader);
            this.readers = new ReadOnlyDictionary<string, IUpmDependencyDeclarationReader>(registrations);
        }
        /// <summary>
        /// Decodes immutable dependency metadata and reports invalid declarations.<br/>
        /// 불변 의존성 메타데이터를 해석하고 잘못된 선언을 보고합니다.
        /// </summary>
        /// <param name="candidate">
        /// The exact package candidate to validate against the metadata.<br/>
        /// 메타데이터와 비교하여 검증할 정확한 패키지 후보입니다.
        /// </param>
        /// <param name="packageJson">
        /// The package.json document from the exact candidate content.<br/>
        /// 정확한 후보 콘텐츠의 package.json 문서입니다.
        /// </param>
        /// <param name="sidecarJson">
        /// An optional runios.package.json document from the same content.<br/>
        /// 동일한 콘텐츠의 선택적인 runios.package.json 문서입니다.
        /// </param>
        /// <returns>
        /// The decoded metadata and structured diagnostics.<br/>
        /// 해석된 메타데이터와 구조화된 진단입니다.
        /// </returns>
        public PackageResult<PackageMetadata> Read(PackageCandidate candidate, string packageJson, string? sidecarJson = null)
        {
            try
            {
                IEnumerable<ScopedRegistryDefinition> inherited = candidate.artifacts.OfType<IUnityManifestArtifact>().Select(x => x.registry).OfType<ScopedRegistryDefinition>();
                UpmSourceConfiguration effectiveSources = new(sources.registries.Concat(inherited), sources.defaultRegistryUrl);
                Dictionary<string, object?> package = JsonData.Parse(packageJson);
                PackageId id = new(AdapterData.PackageName(JsonData.Required(package, "name")));
                if (id != candidate.identity.id) throw new FormatException("package.json name differs from the requested package ID.");
                string version = new UpmVersionConstraint(JsonData.Required(package, "version")).version;
                if (candidate.identity.version is { } expected && expected != version)
                    throw new FormatException("package.json version differs from the candidate version.");
                Dictionary<string, object?> sidecar = sidecarJson is null ? new(StringComparer.Ordinal) : JsonData.Parse(sidecarJson);
                if (sidecarJson is not null && (!sidecar.TryGetValue("schemaVersion", out object? schema) || schema is not JsonNumber number || number.text != "1"))
                    throw new FormatException("runios.package.json requires schemaVersion 1.");
                Dictionary<string, object?> overrides = JsonData.Map(sidecar, "dependencies");
                List<PackageDependency> dependencies = new();
                foreach (var entry in JsonData.Map(package, "dependencies").Where(x => !overrides.ContainsKey(x.Key)).OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    PackageId dependency = new(AdapterData.PackageName(entry.Key));
                    dependencies.Add(new PackageDependency(effectiveSources.VersionRequirement(dependency, JsonData.String(entry.Value),
                        new DeclarationLocation(candidate.identity.sourceKey + "/package.json@" + candidate.identity.revision, "/dependencies/" + entry.Key))));
                }
                foreach (var entry in overrides.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    string kind = JsonData.Required(JsonData.Object(entry.Value), "source");
                    if (!readers.TryGetValue(kind, out IUpmDependencyDeclarationReader? reader) || reader is null)
                        return AdapterData.Failure<PackageMetadata>("upm:unsupported-declaration-source", PackageDiagnosticPhase.Resolution, $"No declaration reader handles '{kind}'.");
                    PackageId dependency = new(AdapterData.PackageName(entry.Key));
                    UpmDeclarationContext context = new(candidate, effectiveSources, new DeclarationLocation(candidate.identity.sourceKey + "/runios.package.json@" + candidate.identity.revision, "/dependencies/" + entry.Key));
                    PackageRequirement requirement = reader.Read(dependency, JsonData.Write(entry.Value), context);
                    if (requirement.id != dependency) throw new FormatException("A declaration reader returned a different package ID.");
                    dependencies.Add(new PackageDependency(requirement));
                }
                return new PackageResult<PackageMetadata>(new PackageMetadata(id, dependencies, JsonData.Optional(package, "displayName"), version));
            }
            catch (Exception exception) when (exception is FormatException || exception is ArgumentException)
            {
                return AdapterData.Failure<PackageMetadata>("upm:invalid-metadata", PackageDiagnosticPhase.Resolution, exception.Message);
            }
        }
    }
}
