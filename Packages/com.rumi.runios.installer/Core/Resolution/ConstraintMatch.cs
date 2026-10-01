#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Resolution
{
    /// <summary>
    /// Reports whether an evaluator accepts a candidate, together with diagnostics.<br/>
    /// 평가기의 후보 허용 여부와 진단을 제공합니다.
    /// </summary>
    public sealed class ConstraintMatch
    {
        /// <summary>
        /// Gets whether the evaluated constraint accepts the candidate.<br/>
        /// 평가한 제약이 후보를 허용하는지 여부를 가져옵니다.
        /// </summary>
        public bool isMatch { get; }
        /// <summary>
        /// Gets the immutable diagnostics accompanying this result.<br/>
        /// 이 결과와 함께 제공되는 불변 진단을 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageDiagnostic> diagnostics { get; }

        /// <summary>
        /// Snapshots a candidate acceptance decision and its diagnostics.<br/>
        /// 후보 허용 결정과 진단을 저장합니다.
        /// </summary>
        /// <param name="isMatch">
        /// Whether the evaluated candidate satisfies the constraint.<br/>
        /// 평가한 후보가 제약을 만족하는지 여부입니다.
        /// </param>
        /// <param name="diagnostics">
        /// Diagnostics accompanying the result or decision.<br/>
        /// 결과 또는 결정과 함께 제공되는 진단입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="diagnostics"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="diagnostics"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public ConstraintMatch(bool isMatch, IEnumerable<PackageDiagnostic>? diagnostics = null)
        {
            this.isMatch = isMatch;
            this.diagnostics = Snapshots.List(diagnostics ?? Array.Empty<PackageDiagnostic>());
        }
    }
}
