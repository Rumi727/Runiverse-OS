#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Resolution
{
    /// <summary>
    /// Supplies a bound source and all currently known requirements to a catalog.<br/>
    /// 바인딩된 출처와 현재 알려진 모든 요구를 카탈로그에 제공합니다.
    /// </summary>
    public sealed class PackageQuery
    {
        /// <summary>
        /// Gets the shared resolution lifetime for consistent provider snapshots.<br/>
        /// 일관된 프로바이더 스냅샷을 위한 공유 해석 수명을 가져옵니다.
        /// </summary>
        public ResolutionSession session { get; }
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId id { get; }
        /// <summary>
        /// Gets the canonical source bound to this catalog query.<br/>
        /// 이 카탈로그 쿼리에 바인딩된 정규화된 출처를 가져옵니다.
        /// </summary>
        public PackageSource source { get; }
        /// <summary>
        /// Gets all currently known requirements for the queried package.<br/>
        /// 조회할 패키지에 대해 현재 알려진 모든 요구를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageRequirement> requirements { get; }

        internal PackageQuery(PackageId id, PackageSource source, IEnumerable<PackageRequirement> requirements, ResolutionSession session)
        {
            this.session = session;
            this.id = id;
            this.source = source;
            this.requirements = Snapshots.List(requirements);
        }
    }
}
