#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Providers;

namespace RuniOS.PackageManagement.Resolution
{
    sealed class ExactRevisionEvaluator : IPackageConstraintEvaluator
    {
        /// <summary>
        /// Gets the routing key for the constraint semantics.<br/>
        /// 선택 제약 의미의 라우팅 키를 가져옵니다.
        /// </summary>
        public string constraintKind => ExactRevisionConstraint.constraintKind;

        public ConstraintMatch Evaluate(PackageConstraint constraint, PackageCandidate candidate)
        {
            if (constraint is ExactRevisionConstraint exact)
                return new ConstraintMatch(exact.revision == candidate.identity.revision);
            return new ConstraintMatch(false, new[]
            {
                PackageDiagnostic.Error("package:invalid-constraint-payload", PackageDiagnosticPhase.Resolution,
                    $"'{constraintKind}' requires an {nameof(ExactRevisionConstraint)} payload.")
            });
        }
    }
}
