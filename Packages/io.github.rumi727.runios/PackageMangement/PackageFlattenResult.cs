#nullable enable
using System;
using System.Collections.Generic;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Contains a flattened closure and graph errors; failed closures must not be executed.<br/>
    /// 평탄화한 closure와 graph 오류를 담으며 실패한 closure는 실행하면 안 됩니다.
    /// </summary>
    public sealed class PackageFlattenResult
    {
        readonly HashSet<PackageIdentity> _required;
        /// <summary>
        /// Gets unique definitions with their root and direct-dependency provenance in unspecified order.<br/>
        /// 고유 정의와 root 및 직접 dependency provenance를 가져오며 순서는 정의되지 않습니다.
        /// </summary>
        public IReadOnlyList<FlattenedPackage> packages { get; }
        /// <summary>
        /// Gets definition and graph errors.<br/>
        /// 정의와 graph 오류를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageGraphDiagnostic> diagnostics { get; }
        /// <summary>
        /// Gets whether the closure is valid for execution.<br/>
        /// closure를 실행할 수 있는지 여부를 가져옵니다.
        /// </summary>
        public bool succeeded => diagnostics.Count == 0;
        internal PackageFlattenResult(List<FlattenedPackage> packages, List<PackageGraphDiagnostic> diagnostics, HashSet<PackageIdentity> required)
        {
            this.packages = packages.AsReadOnly();
            this.diagnostics = diagnostics.AsReadOnly();
            _required = required;
        }
        /// <summary>
        /// Enumerates candidates whose exact definition keys are outside this closure.<br/>
        /// exact 정의 키가 이 closure 밖에 있는 후보를 열거합니다.
        /// </summary>
        /// <param name="candidates">
        /// The definitions to compare, without changing installed packages.<br/>
        /// 설치된 패키지를 변경하지 않고 비교할 정의들입니다.
        /// </param>
        /// <returns>
        /// Definitions absent from the successful closure by exact key.<br/>
        /// exact 키 기준으로 성공한 closure에 없는 정의들을 반환합니다.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown during enumeration when the closure failed.<br/>
        /// closure가 실패했으면 열거 중 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown during enumeration when candidates is null.<br/>
        /// candidates가 null이면 열거 중 발생합니다.
        /// </exception>
        public IEnumerable<IPackage> GetUnused(IEnumerable<IPackage> candidates)
        {
            if (!succeeded) throw new InvalidOperationException("Unused definitions require a successful closure.");
            if (candidates is null) throw new ArgumentNullException(nameof(candidates));
            foreach (IPackage package in candidates)
                if (!_required.Contains(new PackageIdentity(package.id, package.exactIdentity))) yield return package;
        }
    }
}
