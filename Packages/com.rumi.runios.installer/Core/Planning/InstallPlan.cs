#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Planning
{
    /// <summary>
    /// Exposes a validated passive plan, ordered operations, and observed execution preconditions.<br/>
    /// 검증된 수동적 계획, 순서가 정해진 작업 및 관측된 실행 사전 조건을 제공합니다.
    /// </summary>
    public sealed class InstallPlan
    {
        /// <summary>
        /// Gets the management owner to record for executed package changes.<br/>
        /// 실행한 패키지 변경에 기록할 관리 소유자를 가져옵니다.
        /// </summary>
        public string managementOwner { get; }
        /// <summary>
        /// Gets desired installation representations for post-execution verification.<br/>
        /// 실행 후 검증할 원하는 설치 표현을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<PackageId, PackageInstallation> desiredInstallations { get; }
        /// <summary>
        /// Gets the resolved graph or package that the target must represent.<br/>
        /// 대상이 표현해야 하는 해석된 그래프 또는 패키지를 가져옵니다.
        /// </summary>
        public ResolvedPackageGraph desired { get; }
        /// <summary>
        /// Gets the passive environment descriptor associated with these observations or operations.<br/>
        /// 이 관측 또는 작업과 연결된 수동적 환경 설명자를 가져옵니다.
        /// </summary>
        public InstallationTarget target { get; }
        /// <summary>
        /// Gets immutable package transitions selected by the planner.<br/>
        /// 계획기가 선택한 불변 패키지 전이를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageChange> changes { get; }
        /// <summary>
        /// Gets passive operations; validated plans expose them in prerequisite order.<br/>
        /// 수동적 작업을 가져오며 검증된 계획은 선행 순서로 제공합니다.
        /// </summary>
        public IReadOnlyList<PlannedOperation> operations { get; }
        /// <summary>
        /// Gets observed resource fingerprints that an execution runner must revalidate.<br/>
        /// 실행 관리자가 다시 검증해야 하는 관측된 리소스 지문을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<string, string> resourcePreconditions { get; }

        internal InstallPlan(PlanningRequest request, IEnumerable<PackageChange> changes, IEnumerable<PlannedOperation> operations)
        {
            managementOwner = request.managementOwner;
            desiredInstallations = request.installations;
            desired = request.desired;
            target = request.current.target;
            this.changes = Snapshots.List(changes);
            this.operations = Snapshots.List(operations);
            resourcePreconditions = request.current.resourceFingerprints;
        }
    }
}
