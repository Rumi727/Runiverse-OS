#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Providers;
using RuniOS.PackageManagement.Resolution;

namespace RuniOS.PackageManagement.Unity.Editor.Git
{
    /// <summary>
    /// Checks the symbolic references resolved to a candidate commit.<br/>
    /// 후보 commit으로 해석된 symbolic reference를 검사합니다.
    /// </summary>
    public sealed class GitReferenceEvaluator : IPackageConstraintEvaluator
    {
        /// <inheritdoc/>
        public string constraintKind => GitReferenceConstraint.constraintKind;
        /// <inheritdoc/>
        public ConstraintMatch Evaluate(PackageConstraint constraint, PackageCandidate candidate) => constraint is GitReferenceConstraint reference ?
            new ConstraintMatch(candidate.artifacts.OfType<GitCommitArtifact>().Any(x => x.references.Contains(reference.reference, StringComparer.Ordinal))) :
            new ConstraintMatch(false, new[] { PackageDiagnostic.Error("git:invalid-reference-constraint", PackageDiagnosticPhase.Resolution, "A Git reference payload is required.") });
    }
}
