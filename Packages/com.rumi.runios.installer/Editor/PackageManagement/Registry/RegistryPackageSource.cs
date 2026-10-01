#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Internal;

namespace RuniOS.PackageManagement.Unity.Editor.Registry
{
    /// <summary>
    /// Identifies one package on an npm-compatible UPM registry.<br/>
    /// npm 호환 UPM 레지스트리의 패키지 하나를 식별합니다.
    /// </summary>
    public sealed class RegistryPackageSource : PackageSource
    {
        /// <inheritdoc/>
        public const string sourceKind = "upm:registry";
        /// <summary>
        /// Gets the registry endpoint used for metadata lookup.<br/>
        /// 메타데이터 조회에 사용할 레지스트리 주소를 가져옵니다.
        /// </summary>
        public string registryUrl { get; }
        /// <summary>
        /// Gets the logical package identifier.<br/>
        /// 논리적 패키지 식별자를 가져옵니다.
        /// </summary>
        public PackageId packageId { get; }
        /// <summary>
        /// Gets required scoped-registry configuration, or <see langword="null"/> when none is required.<br/>
        /// 필요한 scoped registry 설정을 가져오며 필요하지 않으면 <see langword="null"/>을 반환합니다.
        /// </summary>
        public ScopedRegistryDefinition? registry { get; }
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="packageId">
        /// The logical package identifier.<br/>
        /// 논리적 패키지 식별자입니다.
        /// </param>
        /// <param name="registryUrl">
        /// The registry endpoint used for discovery.<br/>
        /// 검색에 사용할 레지스트리 주소입니다.
        /// </param>
        /// <param name="registry">
        /// Optional scoped-registry configuration for this package.<br/>
        /// 이 패키지의 선택적인 scoped registry 설정입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public RegistryPackageSource(PackageId packageId, string registryUrl, ScopedRegistryDefinition? registry = null)
            : base(sourceKind, AdapterData.HttpUrl(registryUrl) + "/" + AdapterData.PackageName(packageId.value))
        {
            this.packageId = packageId; this.registryUrl = AdapterData.HttpUrl(registryUrl); this.registry = registry;
            if (registry is not null && (registry.url != this.registryUrl || registry.Match(packageId) < 0))
                throw new ArgumentException("The scoped registry must route this package to the source URL.", nameof(registry));
        }
    }
}
