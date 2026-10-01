#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Resolution
{
    /// <summary>
    /// Stores the complete root intent and explicit source overrides for one resolution.<br/>
    /// 한 번의 해석에 사용할 전체 루트 의도와 명시적 출처 재지정을 저장합니다.
    /// </summary>
    public sealed class ResolutionRequest
    {
        /// <summary>
        /// Gets all retained root requirements used for this resolution.<br/>
        /// 이 해석에 사용하는 유지할 전체 루트 요구를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageRequirement> roots { get; }
        /// <summary>
        /// Gets explicit source bindings that override declaration hints.<br/>
        /// 선언 힌트보다 우선하는 명시적 출처 바인딩을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<PackageId, PackageSource> sourceBindings { get; }
        /// <summary>
        /// Gets the upper bound on attempted candidate branches.<br/>
        /// 시도할 후보 분기의 상한을 가져옵니다.
        /// </summary>
        public int maximumCandidateAttempts { get; }

        /// <summary>
        /// Snapshots retained root intent and source overrides with a bounded search budget.<br/>
        /// 제한된 탐색 예산과 함께 유지할 루트 의도와 출처 재지정을 저장합니다.
        /// </summary>
        /// <param name="roots">
        /// All roots that should remain in the resolved closure.<br/>
        /// 해석된 전체 의존성 집합에 유지해야 하는 모든 루트입니다.
        /// </param>
        /// <param name="sourceBindings">
        /// Explicit source overrides indexed by logical package identifier.<br/>
        /// 논리적 패키지 식별자로 색인한 명시적 출처 재지정입니다.
        /// </param>
        /// <param name="maximumCandidateAttempts">
        /// A positive upper bound on attempted candidate branches.<br/>
        /// 시도할 후보 분기에 대한 양수 상한입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when a root or binding is invalid or source binding keys are duplicated.<br/>
        /// 루트 또는 바인딩이 유효하지 않거나 출처 바인딩 키가 중복되면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a numeric or enumeration argument is outside its accepted range.<br/>
        /// 숫자 또는 열거형 인수가 허용 범위를 벗어나면 발생합니다.
        /// </exception>
        public ResolutionRequest(IEnumerable<PackageRequirement> roots,
            IEnumerable<KeyValuePair<PackageId, PackageSource>>? sourceBindings = null, int maximumCandidateAttempts = 1024)
        {
            this.roots = Snapshots.List(roots);
            this.sourceBindings = Snapshots.Map(sourceBindings ?? Array.Empty<KeyValuePair<PackageId, PackageSource>>());
            foreach (PackageId id in this.sourceBindings.Keys)
                Snapshots.Id(id, nameof(sourceBindings));
            if (maximumCandidateAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(maximumCandidateAttempts));
            this.maximumCandidateAttempts = maximumCandidateAttempts;
        }
    }
}
