#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Planning
{
    /// <summary>
    /// Supplies desired state, observed state, installation bindings, and explicit ownership policy.<br/>
    /// 원하는 상태, 관측 상태, 설치 바인딩 및 명시적 소유권 정책을 제공합니다.
    /// </summary>
    public sealed class PlanningRequest
    {
        /// <summary>
        /// Gets the resolved graph or package that the target must represent.<br/>
        /// 대상이 표현해야 하는 해석된 그래프 또는 패키지를 가져옵니다.
        /// </summary>
        public ResolvedPackageGraph desired { get; }
        /// <summary>
        /// Gets the observed environment or package being compared.<br/>
        /// 비교할 관측 환경 또는 패키지를 가져옵니다.
        /// </summary>
        public InstalledEnvironmentSnapshot current { get; }
        /// <summary>
        /// Gets explicit installation bindings for every desired package.<br/>
        /// 원하는 모든 패키지의 명시적 설치 바인딩을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<PackageId, PackageInstallation> installations { get; }
        /// <summary>
        /// Gets the owner key; an absent owner does not grant management ownership.<br/>
        /// 소유자 키를 가져오며 소유자가 없으면 관리 소유권을 부여하지 않습니다.
        /// </summary>
        public string managementOwner { get; }
        /// <summary>
        /// Gets whether unused packages belonging to the selected management owner may be removed.<br/>
        /// 선택한 관리 소유자의 사용하지 않는 패키지를 제거할 수 있는지 여부를 가져옵니다.
        /// </summary>
        public bool removeUnusedOwnedPackages { get; }
        /// <summary>
        /// Gets whether changing externally owned packages is explicitly permitted.<br/>
        /// 외부 소유 패키지 변경을 명시적으로 허용하는지 여부를 가져옵니다.
        /// </summary>
        public bool allowAdoption { get; }

        /// <summary>
        /// Snapshots desired installation bindings and explicit ownership permissions.<br/>
        /// 원하는 설치 바인딩과 명시적 소유권 권한을 저장합니다.
        /// </summary>
        /// <param name="desired">
        /// The complete validated desired graph.<br/>
        /// 완전히 검증된 원하는 그래프입니다.
        /// </param>
        /// <param name="current">
        /// The effective installed environment snapshot.<br/>
        /// 실제 설치 환경 스냅샷입니다.
        /// </param>
        /// <param name="installations">
        /// Explicit bindings for every desired package.<br/>
        /// 원하는 모든 패키지의 명시적 바인딩입니다.
        /// </param>
        /// <param name="managementOwner">
        /// The management owner key; omission denotes external ownership where allowed.<br/>
        /// 관리 소유자 키이며 허용되는 위치에서 생략하면 외부 소유를 나타냅니다.
        /// </param>
        /// <param name="removeUnusedOwnedPackages">
        /// Whether unused packages belonging to this owner may be removed.<br/>
        /// 이 소유자의 사용하지 않는 패키지를 제거할 수 있는지 여부입니다.
        /// </param>
        /// <param name="allowAdoption">
        /// Whether externally owned packages may be changed.<br/>
        /// 외부 소유 패키지를 변경할 수 있는지 여부입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when bindings are invalid or duplicated, or owner text is empty or whitespace.<br/>
        /// 바인딩이 유효하지 않거나 중복되거나 소유자 문자열이 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public PlanningRequest(ResolvedPackageGraph desired, InstalledEnvironmentSnapshot current,
            IEnumerable<KeyValuePair<PackageId, PackageInstallation>> installations, string managementOwner,
            bool removeUnusedOwnedPackages = false, bool allowAdoption = false)
        {
            this.desired = desired ?? throw new ArgumentNullException(nameof(desired));
            this.current = current ?? throw new ArgumentNullException(nameof(current));
            this.installations = Snapshots.Map(installations);
            foreach (PackageId id in this.installations.Keys)
                Snapshots.Id(id, nameof(installations));
            this.managementOwner = Snapshots.Text(managementOwner, nameof(managementOwner));
            this.removeUnusedOwnedPackages = removeUnusedOwnedPackages;
            this.allowAdoption = allowAdoption;
        }
    }
}
