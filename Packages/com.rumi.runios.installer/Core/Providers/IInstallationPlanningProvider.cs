#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Planning;

namespace RuniOS.PackageManagement.Providers
{
    /// <summary>
    /// Plans one installation kind from immutable inputs without target mutation or acquisition.<br/>
    /// 대상 변경이나 콘텐츠 획득 없이 불변 입력으로 한 설치 종류를 계획합니다.
    /// </summary>
    public interface IInstallationPlanningProvider
    {
        /// <summary>
        /// Gets the routing key for the installation mechanism.<br/>
        /// 설치 방법의 라우팅 키를 가져옵니다.
        /// </summary>
        string installationKind { get; }
        /// <summary>
        /// Contributes passive operations for the assigned transitions.<br/>
        /// 할당된 전이에 대한 수동적 작업을 제공합니다.
        /// </summary>
        /// <param name="context">
        /// Global snapshots and the assigned installation and retirement transitions.<br/>
        /// 전체 스냅샷과 할당된 설치 및 제거 전이입니다.
        /// </param>
        /// <returns>
        /// The passive fragment and planning diagnostics.<br/>
        /// 수동적 계획 조각과 계획 진단입니다.
        /// </returns>
        PackageResult<PlanFragment> Plan(InstallationPlanningContext context);
    }
}
