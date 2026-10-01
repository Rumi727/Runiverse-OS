#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Resolution
{
    /// <summary>
    /// Captures candidates in provider preference order and reports search completeness.<br/>
    /// 프로바이더 선호 순서의 후보와 검색 완전성을 저장합니다.
    /// </summary>
    public sealed class CandidateSet
    {
        /// <summary>
        /// Gets exact candidates in deterministic provider preference order.<br/>
        /// 결정적인 프로바이더 선호 순서의 정확한 후보를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageCandidate> candidates { get; }
        /// <summary>
        /// Gets whether discovery covered the complete requested candidate or installed state.<br/>
        /// 검색이 요청한 후보 또는 설치 상태 전체를 포함하는지 여부를 가져옵니다.
        /// </summary>
        public bool isComplete { get; }

        /// <summary>
        /// Snapshots exact candidates in provider preference order.<br/>
        /// 프로바이더 선호 순서로 정확한 후보를 저장합니다.
        /// </summary>
        /// <param name="candidates">
        /// Exact candidates in deterministic preference order.<br/>
        /// 결정적인 선호 순서의 정확한 후보입니다.
        /// </param>
        /// <param name="isComplete">
        /// Whether discovery covers the complete requested state.<br/>
        /// 검색이 요청한 전체 상태를 포함하는지 여부입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="candidates"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="candidates"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public CandidateSet(IEnumerable<PackageCandidate> candidates, bool isComplete = true)
        {
            this.candidates = Snapshots.List(candidates);
            this.isComplete = isComplete;
        }
    }
}
