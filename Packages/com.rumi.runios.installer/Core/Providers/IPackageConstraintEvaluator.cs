#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Resolution;

namespace RuniOS.PackageManagement.Providers
{
    /// <summary>
    /// Evaluates selection semantics independently of candidate discovery.<br/>
    /// 후보 검색과 독립적으로 선택 제약의 의미를 평가합니다.
    /// </summary>
    public interface IPackageConstraintEvaluator
    {
        /// <summary>
        /// Gets the routing key for the constraint semantics.<br/>
        /// 선택 제약 의미의 라우팅 키를 가져옵니다.
        /// </summary>
        string constraintKind { get; }
        /// <summary>
        /// Evaluates the constraint against an exact candidate without mutation.<br/>
        /// 변경 없이 정확한 후보에 대해 제약을 평가합니다.
        /// </summary>
        /// <param name="constraint">
        /// The immutable selection constraint.<br/>
        /// 불변 선택 제약입니다.
        /// </param>
        /// <param name="candidate">
        /// The exact candidate to evaluate.<br/>
        /// 평가할 정확한 후보입니다.
        /// </param>
        /// <returns>
        /// The acceptance decision and diagnostics.<br/>
        /// 허용 결정과 진단입니다.
        /// </returns>
        ConstraintMatch Evaluate(PackageConstraint constraint, PackageCandidate candidate);
    }
}
