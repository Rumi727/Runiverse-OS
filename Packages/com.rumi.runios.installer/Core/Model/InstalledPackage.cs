#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Captures an effective installed package and its management ownership.<br/>
    /// 실제 사용 중인 설치 패키지와 관리 소유권을 저장합니다.
    /// </summary>
    public sealed class InstalledPackage
    {
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId id { get; }
        /// <summary>
        /// Gets the exact package identity; installed observations may report an unknown identity.<br/>
        /// 정확한 패키지 식별자를 가져오며 설치 관측은 알 수 없는 식별자를 보고할 수 있습니다.
        /// </summary>
        public PackageIdentity? identity { get; }
        /// <summary>
        /// Gets the passive representation used by the installed package.<br/>
        /// 설치 패키지가 사용하는 수동적 표현을 가져옵니다.
        /// </summary>
        public PackageInstallation installation { get; }
        /// <summary>
        /// Gets the owner key; an absent owner does not grant management ownership.<br/>
        /// 소유자 키를 가져오며 소유자가 없으면 관리 소유권을 부여하지 않습니다.
        /// </summary>
        public string? managementOwner { get; }
        /// <summary>
        /// Gets immutable dependency declarations, edges, or effective observed identifiers.<br/>
        /// 불변 의존성 선언, 간선 또는 실제 관측된 식별자를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageId> dependencies { get; }
        /// <summary>
        /// Gets observed dependency declarations used to verify changes required by retained packages.<br/>
        /// 유지할 패키지가 요구하는 변경을 검증할 때 사용할 관측된 의존성 선언을 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageRequirement> dependencyRequirements { get; }

        /// <summary>
        /// Snapshots effective identity, installation representation, and observed dependencies.<br/>
        /// 실제 식별자, 설치 표현 및 관측된 의존성을 저장합니다.
        /// </summary>
        /// <param name="id">
        /// The initialized logical package or operation identifier.<br/>
        /// 초기화된 논리적 패키지 또는 작업 식별자입니다.
        /// </param>
        /// <param name="identity">
        /// The exact identity, or an unknown identity for installed observations.<br/>
        /// 정확한 식별자 또는 설치 관측의 알 수 없는 식별자입니다.
        /// </param>
        /// <param name="installation">
        /// The effective passive installation representation.<br/>
        /// 실제 수동적 설치 표현입니다.
        /// </param>
        /// <param name="managementOwner">
        /// The management owner key; omission denotes external ownership where allowed.<br/>
        /// 관리 소유자 키이며 허용되는 위치에서 생략하면 외부 소유를 나타냅니다.
        /// </param>
        /// <param name="dependencies">
        /// Dependency observations or declarations; omitted values produce an empty snapshot.<br/>
        /// 의존성 관측 또는 선언이며 생략하면 빈 스냅샷을 생성합니다.
        /// </param>
        /// <param name="dependencyRequirements">
        /// Observed dependency declarations; omission leaves requirement compatibility unknown.<br/>
        /// 관측된 의존성 선언이며 생략하면 요구 호환성을 알 수 없는 상태로 둡니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when identifiers disagree or are uninitialized, owner text is empty, or a declaration is <see langword="null"/>.<br/>
        /// 식별자가 일치하지 않거나 초기화되지 않았거나 소유자 문자열이 비어 있거나 선언이 <see langword="null"/>이면 발생합니다.
        /// </exception>
        public InstalledPackage(PackageId id, PackageIdentity? identity, PackageInstallation installation,
            string? managementOwner = null, IEnumerable<PackageId>? dependencies = null,
            IEnumerable<PackageRequirement>? dependencyRequirements = null)
        {
            this.id = Snapshots.Id(id, nameof(id));
            if (identity is not null && identity.id != id)
                throw new ArgumentException("The installed identity must match the package identifier.", nameof(identity));
            this.identity = identity;
            this.installation = installation ?? throw new ArgumentNullException(nameof(installation));
            this.managementOwner = managementOwner is null ? null : Snapshots.Text(managementOwner, nameof(managementOwner));
            this.dependencyRequirements = Snapshots.List(dependencyRequirements ?? Array.Empty<PackageRequirement>());
            this.dependencies = Snapshots.List((dependencies ?? Array.Empty<PackageId>())
                .Concat(this.dependencyRequirements.Select(x => x.id)).Distinct());
            foreach (PackageId dependency in this.dependencies)
                Snapshots.Id(dependency, nameof(dependencies));
        }
    }
}
