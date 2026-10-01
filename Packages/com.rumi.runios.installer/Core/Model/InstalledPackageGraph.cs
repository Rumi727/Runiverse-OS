#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Exposes immutable effective observations, including unknown revisions and incomplete discovery.<br/>
    /// 알 수 없는 리비전과 불완전한 조회를 포함하는 불변 관측 결과를 제공합니다.
    /// </summary>
    public sealed class InstalledPackageGraph
    {
        /// <summary>
        /// Gets effective installed observations indexed by logical package identifier.<br/>
        /// 논리적 패키지 식별자로 인덱싱한 실제 설치 관측을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<PackageId, InstalledPackage> packages { get; }
        /// <summary>
        /// Gets whether discovery covered the complete requested candidate or installed state.<br/>
        /// 검색이 요청한 후보 또는 설치 상태 전체를 포함하는지 여부를 가져옵니다.
        /// </summary>
        public bool isComplete { get; }

        InstalledPackageGraph(Dictionary<PackageId, InstalledPackage> packages, bool isComplete)
        {
            this.packages = Snapshots.Map(packages);
            this.isComplete = isComplete;
        }

        /// <summary>
        /// Freezes normalized observations and reports duplicate effective package identifiers.<br/>
        /// 정규화된 관측을 고정하고 실제 패키지 식별자의 중복을 진단합니다.
        /// </summary>
        /// <param name="packages">
        /// Effective packages after environment precedence has been applied.<br/>
        /// 환경 우선순위를 적용한 실제 패키지입니다.
        /// </param>
        /// <param name="isComplete">
        /// Whether package discovery and dependency observations are complete.<br/>
        /// 패키지 조회와 의존성 관측이 완전한지 여부입니다.
        /// </param>
        /// <returns>
        /// The graph or diagnostics when effective identifiers are duplicated.<br/>
        /// 그래프를 반환하거나 실제 식별자가 중복되면 진단을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="packages"/> is <see langword="null"/>.<br/>
        /// <paramref name="packages"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="packages"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="packages"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public static PackageResult<InstalledPackageGraph> Create(IEnumerable<InstalledPackage> packages, bool isComplete = true)
        {
            IReadOnlyList<InstalledPackage> snapshot = Snapshots.List(packages);
            Dictionary<PackageId, InstalledPackage> indexed = new();
            List<PackageDiagnostic> diagnostics = new();
            foreach (InstalledPackage package in snapshot)
            {
                if (!indexed.TryAdd(package.id, package))
                    diagnostics.Add(PackageDiagnostic.Error("package:duplicate-installed-id", PackageDiagnosticPhase.Observation,
                        $"Effective observations contain duplicate package '{package.id}'.", new[] { package.id }));
            }
            if (diagnostics.Count > 0)
                return new PackageResult<InstalledPackageGraph>(null, diagnostics);
            return new PackageResult<InstalledPackageGraph>(new InstalledPackageGraph(indexed, isComplete));
        }
    }
}
