#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Exposes one validated selection without installation instructions.<br/>
    /// 설치 지시 없이 검증된 선택 하나를 제공합니다.
    /// </summary>
    public sealed class ResolvedPackage
    {
        /// <summary>
        /// Gets the exact package identity; installed observations may report an unknown identity.<br/>
        /// 정확한 패키지 식별자를 가져오며 설치 관측은 알 수 없는 식별자를 보고할 수 있습니다.
        /// </summary>
        public PackageIdentity identity { get; }
        /// <summary>
        /// Gets dependency metadata retrieved for the selected exact revision.<br/>
        /// 선택한 정확한 리비전에서 조회한 의존성 메타데이터를 가져옵니다.
        /// </summary>
        public PackageMetadata metadata { get; }
        /// <summary>
        /// Gets immutable content references without acquiring their content.<br/>
        /// 콘텐츠를 획득하지 않고 불변 콘텐츠 참조를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageArtifactReference> artifacts { get; }

        internal ResolvedPackage(PackageCandidate candidate, PackageMetadata metadata)
        {
            identity = candidate.identity;
            artifacts = candidate.artifacts;
            this.metadata = metadata;
        }
    }
}
