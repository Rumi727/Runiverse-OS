#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Planning;

namespace RuniOS.PackageManagement.Providers
{
    /// <summary>
    /// Combines shared environment edits before generic ordering and conflict validation.<br/>
    /// 공통 순서 및 충돌 검증 전에 공유 환경 변경을 합성합니다.
    /// </summary>
    /// <remarks>
    /// Rewrites operation references and package boundaries when merging operations. Must not mutate the target.<br/>
    /// 작업을 병합할 때 작업 참조와 패키지 경계를 갱신해야 하며 대상을 변경하면 안 됩니다.
    /// </remarks>
    public interface IPlanFragmentComposer
    {
        /// <summary>
        /// Gets the canonical location or configuration key used for descriptor comparison.<br/>
        /// 설명자 비교에 사용하는 정규화된 위치 또는 설정 키를 가져옵니다.
        /// </summary>
        string key { get; }
        /// <summary>
        /// Merges shared edits while rewriting their operation and package references.<br/>
        /// 작업 및 패키지 참조를 갱신하면서 공유 변경을 병합합니다.
        /// </summary>
        /// <param name="request">
        /// The immutable planning input.<br/>
        /// 불변 계획 입력입니다.
        /// </param>
        /// <param name="fragments">
        /// The fragments to normalize.<br/>
        /// 정규화할 계획 조각입니다.
        /// </param>
        /// <returns>
        /// Normalized fragments and diagnostics.<br/>
        /// 정규화된 계획 조각과 진단입니다.
        /// </returns>
        PackageResult<PlanFragmentSet> Compose(PlanningRequest request, PlanFragmentSet fragments);
    }
}
