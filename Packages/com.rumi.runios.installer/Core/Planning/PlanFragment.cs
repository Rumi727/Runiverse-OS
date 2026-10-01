#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Planning
{
    /// <summary>
    /// Returns passive operations and the package lifecycle boundaries they implement.<br/>
    /// 구현할 패키지 생명주기 경계와 수동적 작업을 반환합니다.
    /// </summary>
    public sealed class PlanFragment
    {
        /// <summary>
        /// Gets passive operations; validated plans expose them in prerequisite order.<br/>
        /// 수동적 작업을 가져오며 검증된 계획은 선행 순서로 제공합니다.
        /// </summary>
        public IReadOnlyList<PlannedOperation> operations { get; }
        /// <summary>
        /// Gets exported package lifecycle boundaries for cross-provider ordering.<br/>
        /// 프로바이더 간 순서에 사용할 패키지 생명주기 경계를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageOperationBoundary> boundaries { get; }

        /// <summary>
        /// Snapshots contributed operations and exported package boundaries.<br/>
        /// 제공된 작업과 패키지 경계를 저장합니다.
        /// </summary>
        /// <param name="operations">
        /// Passive contributed operations.<br/>
        /// 제공된 수동적 작업입니다.
        /// </param>
        /// <param name="boundaries">
        /// Exported package lifecycle boundaries.<br/>
        /// 제공할 패키지 생명주기 경계입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an operation or boundary is <see langword="null"/>.<br/>
        /// 작업 또는 경계가 <see langword="null"/>이면 발생합니다.
        /// </exception>
        public PlanFragment(IEnumerable<PlannedOperation> operations, IEnumerable<PackageOperationBoundary> boundaries)
        {
            this.operations = Snapshots.List(operations);
            this.boundaries = Snapshots.List(boundaries);
        }
    }
}
