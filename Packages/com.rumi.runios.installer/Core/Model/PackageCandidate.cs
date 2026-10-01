#nullable enable
using System;
using System.Collections.Generic;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Provides an immutable candidate identity and content references.<br/>
    /// 불변 후보 식별자와 콘텐츠 참조를 제공합니다.
    /// </summary>
    public sealed class PackageCandidate
    {
        /// <summary>
        /// Gets the exact package identity; installed observations may report an unknown identity.<br/>
        /// 정확한 패키지 식별자를 가져오며 설치 관측은 알 수 없는 식별자를 보고할 수 있습니다.
        /// </summary>
        public PackageIdentity identity { get; }
        /// <summary>
        /// Gets immutable content references without acquiring their content.<br/>
        /// 콘텐츠를 획득하지 않고 불변 콘텐츠 참조를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageArtifactReference> artifacts { get; }

        /// <summary>
        /// Snapshots content references associated with an exact identity.<br/>
        /// 정확한 식별자와 연결된 콘텐츠 참조를 스냅샷으로 저장합니다.
        /// </summary>
        /// <param name="identity">
        /// The exact identity associated with the candidate.<br/>
        /// 후보와 연결된 정확한 식별자입니다.
        /// </param>
        /// <param name="artifacts">
        /// Immutable content references; omitted values produce an empty snapshot.<br/>
        /// 불변 콘텐츠 참조이며 생략하면 빈 스냅샷을 생성합니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="artifacts"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="artifacts"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public PackageCandidate(PackageIdentity identity, IEnumerable<PackageArtifactReference>? artifacts = null)
        {
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.artifacts = Snapshots.List(artifacts ?? Array.Empty<PackageArtifactReference>());
        }
    }
}
