#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Manifest;
using RuniOS.PackageManagement.Unity.Editor.Metadata;

namespace RuniOS.PackageManagement.Unity.Editor.Registry
{
    /// <summary>
    /// Retains an exact registry version and its declared distribution evidence.<br/>
    /// 정확한 레지스트리 버전과 선언된 배포 증거를 보존합니다.
    /// </summary>
    public sealed class RegistryPackageArtifact : PackageArtifactReference, IUnityManifestArtifact
    {
        /// <summary>
        /// Identifies the capability routing key.<br/>
        /// 기능 연결 키를 식별합니다.
        /// </summary>
        public const string artifactKind = "upm:registry-package";
        /// <summary>
        /// Gets the immutable acquisition-source descriptor.<br/>
        /// 불변 획득 출처 설명자를 가져옵니다.
        /// </summary>
        public RegistryPackageSource source { get; }
        /// <summary>
        /// Gets the exact declared UPM version.<br/>
        /// 정확하게 선언된 UPM 버전을 가져옵니다.
        /// </summary>
        public string version { get; }
        /// <summary>
        /// Gets published tarball integrity evidence, if available.<br/>
        /// 사용 가능한 배포 tarball 무결성 증거를 가져옵니다.
        /// </summary>
        public string? integrity { get; }
        /// <summary>
        /// Gets the published tarball location, if available.<br/>
        /// 사용 가능한 배포 tarball 위치를 가져옵니다.
        /// </summary>
        public string? tarballUrl { get; }
        /// <inheritdoc/>
        public ScopedRegistryDefinition? registry { get; }
        /// <inheritdoc/>
        public string dependencyValue => version;
        /// <inheritdoc/>
        public string packageIdentifier { get; }
        /// <inheritdoc/>
        public PackageIdentity GetInstalledIdentity(PackageId id) => new(id, RegistryPackageSource.sourceKind, source.key, version, version);
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="source">
        /// The immutable acquisition-source descriptor.<br/>
        /// 불변 획득 출처 설명자입니다.
        /// </param>
        /// <param name="version">
        /// The exact UPM version, excluding version ranges.<br/>
        /// 버전 범위를 제외한 정확한 UPM 버전입니다.
        /// </param>
        /// <param name="integrity">
        /// Optional published tarball integrity metadata.<br/>
        /// 선택적인 배포 tarball 무결성 메타데이터입니다.
        /// </param>
        /// <param name="tarballUrl">
        /// Optional published tarball location.<br/>
        /// 선택적인 배포 tarball 위치입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public RegistryPackageArtifact(RegistryPackageSource source, string version, string? integrity = null, string? tarballUrl = null) : base(artifactKind)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.version = new UpmVersionConstraint(version).version;
            this.integrity = integrity; this.tarballUrl = tarballUrl; registry = source.registry;
            packageIdentifier = source.packageId.value + "@" + version;
        }
    }
}
