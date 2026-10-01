#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Planning
{
    /// <summary>
    /// Supplies global context and the installation or retirement transitions assigned to one provider.<br/>
    /// 한 프로바이더에 할당한 설치 또는 제거 전이와 전체 문맥을 제공합니다.
    /// </summary>
    public sealed class InstallationPlanningContext
    {
        /// <summary>
        /// Gets the full immutable planning context shared across providers.<br/>
        /// 프로바이더 간 공유하는 전체 불변 계획 문맥을 가져옵니다.
        /// </summary>
        public PlanningRequest request { get; }
        /// <summary>
        /// Gets the routing key for the installation mechanism.<br/>
        /// 설치 방법의 라우팅 키를 가져옵니다.
        /// </summary>
        public string installationKind { get; }
        /// <summary>
        /// Gets the install or update transitions assigned to this provider.<br/>
        /// 이 프로바이더에 할당한 설치 또는 갱신 전이를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageChange> installChanges { get; }
        /// <summary>
        /// Gets the retirement transitions assigned to this provider.<br/>
        /// 이 프로바이더에 할당한 제거 전이를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageChange> removeChanges { get; }

        internal InstallationPlanningContext(PlanningRequest request, string kind, IEnumerable<PackageChange> changes)
        {
            this.request = request;
            installationKind = kind;
            installChanges = Snapshots.List(changes.Where(x => x.desiredInstallation?.kind == kind));
            removeChanges = Snapshots.List(changes.Where(x => x.current is { } current && current.installation.kind == kind &&
                (x.desiredInstallation is null || !Snapshots.SameInstallation(current.installation, x.desiredInstallation))));
        }
    }
}
