#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Planning
{
    /// <summary>
    /// Describes one required change for preview and installation providers.<br/>
    /// 미리 보기와 설치 프로바이더에 필요한 변경 하나를 나타냅니다.
    /// </summary>
    public sealed class PackageChange
    {
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId id { get; }
        /// <summary>
        /// Gets the routing key or transition category consumed by the corresponding provider.<br/>
        /// 해당 프로바이더가 사용하는 라우팅 키 또는 전이 분류를 가져옵니다.
        /// </summary>
        public PackageChangeKind kind { get; }
        /// <summary>
        /// Gets the observed environment or package being compared.<br/>
        /// 비교할 관측 환경 또는 패키지를 가져옵니다.
        /// </summary>
        public InstalledPackage? current { get; }
        /// <summary>
        /// Gets the resolved graph or package that the target must represent.<br/>
        /// 대상이 표현해야 하는 해석된 그래프 또는 패키지를 가져옵니다.
        /// </summary>
        public ResolvedPackage? desired { get; }
        /// <summary>
        /// Gets the requested installation representation, absent for removals.<br/>
        /// 요청된 설치 표현을 가져오며 제거 시에는 존재하지 않습니다.
        /// </summary>
        public PackageInstallation? desiredInstallation { get; }

        internal PackageChange(PackageId id, PackageChangeKind kind, InstalledPackage? current,
            ResolvedPackage? desired, PackageInstallation? desiredInstallation)
        {
            this.id = id;
            this.kind = kind;
            this.current = current;
            this.desired = desired;
            this.desiredInstallation = desiredInstallation;
        }
    }
}
