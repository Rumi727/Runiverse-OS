#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Planning
{
    /// <summary>
    /// Supplies an immutable fragment collection to registered composers.<br/>
    /// 등록된 합성기에 불변 계획 조각 컬렉션을 제공합니다.
    /// </summary>
    public sealed class PlanFragmentSet
    {
        /// <summary>
        /// Gets the immutable fragment snapshots supplied to a composer.<br/>
        /// 합성기에 제공하는 불변 계획 조각 스냅샷을 가져옵니다.
        /// </summary>
        public IReadOnlyList<PlanFragment> fragments { get; }

        /// <summary>
        /// Snapshots fragments without executing or merging their operations.<br/>
        /// 작업을 실행하거나 병합하지 않고 계획 조각을 저장합니다.
        /// </summary>
        /// <param name="fragments">
        /// Passive plan fragments.<br/>
        /// 수동적 계획 조각입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="fragments"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="fragments"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public PlanFragmentSet(IEnumerable<PlanFragment> fragments) => this.fragments = Snapshots.List(fragments);
    }
}
