#nullable enable
using System.Linq;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Providers;
using RuniOS.PackageManagement.Resolution;

namespace RuniOS.PackageManagement.Unity.Editor.Metadata
{
    /// <summary>
    /// Checks exact UPM versions independently of package source.<br/>
    /// 패키지 출처와 독립적으로 정확한 UPM 버전을 검사합니다.
    /// </summary>
    public sealed class UpmVersionEvaluator : IPackageConstraintEvaluator
    {
        /// <inheritdoc/>
        public string constraintKind => UpmVersionConstraint.constraintKind;
        /// <inheritdoc/>
        public ConstraintMatch Evaluate(PackageConstraint constraint, PackageCandidate candidate) =>
            constraint is UpmVersionConstraint exact ? new ConstraintMatch(candidate.identity.version == exact.version) :
            new ConstraintMatch(false, new[] { PackageDiagnostic.Error("upm:invalid-version-constraint", PackageDiagnosticPhase.Resolution, "An exact UPM version payload is required.") });
    }
}
