#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Captures effective packages, resource fingerprints, and passive environment facts.<br/>
    /// 실제 패키지, 리소스 지문 및 수동적 환경 정보를 저장합니다.
    /// </summary>
    public sealed class InstalledEnvironmentSnapshot
    {
        /// <summary>
        /// Gets the passive environment descriptor associated with these observations or operations.<br/>
        /// 이 관측 또는 작업과 연결된 수동적 환경 설명자를 가져옵니다.
        /// </summary>
        public InstallationTarget target { get; }
        /// <summary>
        /// Gets the immutable effective package graph or its package index.<br/>
        /// 불변 실제 패키지 그래프 또는 패키지 색인을 가져옵니다.
        /// </summary>
        public InstalledPackageGraph packages { get; }
        /// <summary>
        /// Gets observed opaque resource fingerprints for later execution precondition checks.<br/>
        /// 이후 실행 전제조건 검사에 사용할 관측된 불투명 리소스 지문을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<string, string> resourceFingerprints { get; }
        /// <summary>
        /// Gets immutable environment facts interpreted only by matching adapters.<br/>
        /// 해당 어댑터만 해석하는 불변 환경 정보를 가져옵니다.
        /// </summary>
        public IReadOnlyList<EnvironmentFact> facts { get; }

        /// <summary>
        /// Snapshots observed packages, resources, and environment facts.<br/>
        /// 관측한 패키지, 리소스 및 환경 정보를 저장합니다.
        /// </summary>
        /// <param name="target">
        /// The passive environment descriptor.<br/>
        /// 수동적 환경 설명자입니다.
        /// </param>
        /// <param name="packages">
        /// Normalized effective package observations.<br/>
        /// 정규화된 실제 패키지 관측입니다.
        /// </param>
        /// <param name="resourceFingerprints">
        /// Observed opaque resource states.<br/>
        /// 관측된 불투명 리소스 상태입니다.
        /// </param>
        /// <param name="facts">
        /// Immutable adapter-specific environment facts.<br/>
        /// 불변 어댑터별 환경 정보입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when resource entries are invalid or duplicated, or a fact is <see langword="null"/>.<br/>
        /// 리소스 항목이 유효하지 않거나 중복되거나 환경 정보가 <see langword="null"/>이면 발생합니다.
        /// </exception>
        public InstalledEnvironmentSnapshot(InstallationTarget target, InstalledPackageGraph packages,
            IEnumerable<KeyValuePair<string, string>>? resourceFingerprints = null, IEnumerable<EnvironmentFact>? facts = null)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.packages = packages ?? throw new ArgumentNullException(nameof(packages));
            this.resourceFingerprints = Snapshots.Map(resourceFingerprints ?? Array.Empty<KeyValuePair<string, string>>());
            foreach (string resource in this.resourceFingerprints.Keys)
                Snapshots.Text(resource, nameof(resourceFingerprints));
            this.facts = Snapshots.List(facts ?? Array.Empty<EnvironmentFact>());
        }
    }
}
