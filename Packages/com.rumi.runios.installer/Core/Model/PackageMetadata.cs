#nullable enable
using System;
using System.Collections.Generic;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Provides the dependency declarations retrieved for one exact candidate.<br/>
    /// 정확한 후보 하나에서 조회한 의존성 선언을 제공합니다.
    /// </summary>
    public sealed class PackageMetadata
    {
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId id { get; }
        /// <summary>
        /// Gets display text independent of frontend localization services.<br/>
        /// 프런트엔드 지역화 서비스와 독립적인 표시 문자열을 가져옵니다.
        /// </summary>
        public string displayName { get; }
        /// <summary>
        /// Gets optional version metadata without changing revision identity equality.<br/>
        /// 리비전 식별자의 동등성을 변경하지 않는 선택적 버전 메타데이터를 가져옵니다.
        /// </summary>
        public string? version { get; }
        /// <summary>
        /// Gets immutable dependency declarations, edges, or effective observed identifiers.<br/>
        /// 불변 의존성 선언, 간선 또는 실제 관측된 식별자를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageDependency> dependencies { get; }

        /// <summary>
        /// Snapshots dependency declarations from one exact candidate revision.<br/>
        /// 정확한 후보 리비전 하나의 의존성 선언을 스냅샷으로 저장합니다.
        /// </summary>
        /// <param name="id">
        /// The initialized logical package or operation identifier.<br/>
        /// 초기화된 논리적 패키지 또는 작업 식별자입니다.
        /// </param>
        /// <param name="dependencies">
        /// Dependency observations or declarations; omitted values produce an empty snapshot.<br/>
        /// 의존성 관측 또는 선언이며 생략하면 빈 스냅샷을 생성합니다.
        /// </param>
        /// <param name="displayName">
        /// Optional display text; omission uses the logical package identifier.<br/>
        /// 선택적 표시 문자열이며 생략하면 논리적 패키지 식별자를 사용합니다.
        /// </param>
        /// <param name="version">
        /// Optional descriptive version metadata.<br/>
        /// 선택적 설명용 버전 메타데이터입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when the identifier is uninitialized, a dependency is <see langword="null"/>, or supplied version text is empty.<br/>
        /// 식별자가 초기화되지 않았거나 의존성이 <see langword="null"/>이거나 제공한 버전 문자열이 비어 있으면 발생합니다.
        /// </exception>
        public PackageMetadata(PackageId id, IEnumerable<PackageDependency>? dependencies = null, string? displayName = null, string? version = null)
        {
            this.id = Snapshots.Id(id, nameof(id));
            this.displayName = displayName ?? id.value;
            this.version = version is null ? null : Snapshots.Text(version, nameof(version));
            this.dependencies = Snapshots.List(dependencies ?? Array.Empty<PackageDependency>());
        }
    }
}
