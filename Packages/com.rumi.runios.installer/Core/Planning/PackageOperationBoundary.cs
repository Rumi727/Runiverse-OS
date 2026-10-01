#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Planning
{
    /// <summary>
    /// Exports operation boundaries for cross-provider dependency and replacement ordering.<br/>
    /// 프로바이더 간 의존성 및 교체 순서에 사용할 작업 경계를 제공합니다.
    /// </summary>
    /// <remarks>
    /// Atomic operations may be shared by multiple package boundaries. Boundaries must expose all target-changing operations.<br/>
    /// 여러 패키지 경계가 하나의 원자적 작업을 공유할 수 있으며 경계는 대상 변경 작업을 모두 포함해야 합니다.
    /// </remarks>
    public sealed class PackageOperationBoundary
    {
        /// <summary>
        /// Gets the package whose lifecycle boundary is exported.<br/>
        /// 생명주기 경계를 제공할 패키지를 가져옵니다.
        /// </summary>
        public PackageId packageId { get; }
        /// <summary>
        /// Gets the diagnostic stage or package lifecycle phase.<br/>
        /// 진단 단계 또는 패키지 생명주기 단계를 가져옵니다.
        /// </summary>
        public PackageOperationPhase phase { get; }
        /// <summary>
        /// Gets operations that begin target changes for this lifecycle phase.<br/>
        /// 이 생명주기 단계의 대상 변경을 시작하는 작업을 가져옵니다.
        /// </summary>
        public IReadOnlyList<string> startOperations { get; }
        /// <summary>
        /// Gets operations whose completion establishes the lifecycle phase.<br/>
        /// 완료 시 생명주기 단계가 성립되는 작업을 가져옵니다.
        /// </summary>
        public IReadOnlyList<string> completionOperations { get; }

        /// <summary>
        /// Snapshots target-change entry and completion operations for one package phase.<br/>
        /// 패키지 단계 하나의 대상 변경 시작 및 완료 작업을 저장합니다.
        /// </summary>
        /// <param name="packageId">
        /// The package whose lifecycle phase is represented.<br/>
        /// 생명주기 단계를 표현할 패키지입니다.
        /// </param>
        /// <param name="phase">
        /// The diagnostic stage or package lifecycle phase.<br/>
        /// 진단 단계 또는 패키지 생명주기 단계입니다.
        /// </param>
        /// <param name="startOperations">
        /// Target-changing entry operations for the phase.<br/>
        /// 단계의 대상 변경 시작 작업입니다.
        /// </param>
        /// <param name="completionOperations">
        /// Operations establishing phase completion.<br/>
        /// 단계 완료를 성립시키는 작업입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the package identifier is uninitialized or operation identifiers are empty or invalid.<br/>
        /// 패키지 식별자가 초기화되지 않았거나 작업 식별자가 비어 있거나 유효하지 않으면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a numeric or enumeration argument is outside its accepted range.<br/>
        /// 숫자 또는 열거형 인수가 허용 범위를 벗어나면 발생합니다.
        /// </exception>
        public PackageOperationBoundary(PackageId packageId, PackageOperationPhase phase,
            IEnumerable<string> startOperations, IEnumerable<string> completionOperations)
        {
            this.packageId = Snapshots.Id(packageId, nameof(packageId));
            if (phase != PackageOperationPhase.Install && phase != PackageOperationPhase.Remove)
                throw new ArgumentOutOfRangeException(nameof(phase));
            this.phase = phase;
            this.startOperations = Snapshots.List(startOperations);
            this.completionOperations = Snapshots.List(completionOperations);
            foreach (string id in this.startOperations.Concat(this.completionOperations))
                Snapshots.Text(id, nameof(startOperations));
        }
    }
}
