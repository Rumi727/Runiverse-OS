#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Planning
{
    /// <summary>
    /// Couples an operation payload to immutable prerequisite and resource declarations.<br/>
    /// 작업 데이터를 불변 선행 작업 및 리소스 선언과 연결합니다.
    /// </summary>
    public sealed class PlannedOperation
    {
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public string id { get; }
        /// <summary>
        /// Gets the passive payload executed by a matching executor.<br/>
        /// 해당 실행기가 실행할 수동적 데이터를 가져옵니다.
        /// </summary>
        public InstallOperation operation { get; }
        /// <summary>
        /// Gets operation identifiers that must complete before this operation.<br/>
        /// 이 작업 전에 완료해야 하는 작업 식별자를 가져옵니다.
        /// </summary>
        public IReadOnlyList<string> prerequisites { get; }
        /// <summary>
        /// Gets resource access declarations used for conflict detection.<br/>
        /// 충돌 탐지에 사용할 리소스 접근 선언을 가져옵니다.
        /// </summary>
        public IReadOnlyList<ResourceClaim> claims { get; }

        /// <summary>
        /// Snapshots operation prerequisites and declared resource access.<br/>
        /// 작업의 선행 조건과 선언된 리소스 접근을 저장합니다.
        /// </summary>
        /// <param name="id">
        /// The initialized logical package or operation identifier.<br/>
        /// 초기화된 논리적 패키지 또는 작업 식별자입니다.
        /// </param>
        /// <param name="operation">
        /// The immutable operation payload.<br/>
        /// 불변 작업 데이터입니다.
        /// </param>
        /// <param name="prerequisites">
        /// Operation identifiers that must complete first.<br/>
        /// 먼저 완료해야 하는 작업 식별자입니다.
        /// </param>
        /// <param name="claims">
        /// Declared abstract resource access.<br/>
        /// 선언된 추상 리소스 접근입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when operation identifiers are empty or whitespace, or resource claims contain a <see langword="null"/> element.<br/>
        /// 작업 식별자가 비어 있거나 공백이거나 리소스 접근 선언에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public PlannedOperation(string id, InstallOperation operation, IEnumerable<string>? prerequisites = null,
            IEnumerable<ResourceClaim>? claims = null)
        {
            this.id = Snapshots.Text(id, nameof(id));
            this.operation = operation ?? throw new ArgumentNullException(nameof(operation));
            this.prerequisites = Snapshots.List(prerequisites ?? Array.Empty<string>());
            foreach (string prerequisite in this.prerequisites)
                Snapshots.Text(prerequisite, nameof(prerequisites));
            this.claims = Snapshots.List(claims ?? Array.Empty<ResourceClaim>());
        }
    }
}
