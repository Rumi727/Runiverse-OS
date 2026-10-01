#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Resolution;

namespace RuniOS.PackageManagement.Providers
{
    /// <summary>
    /// Resolves the complete closure or returns diagnostics without an executable graph.<br/>
    /// 전체 의존성 집합을 해석하거나 실행 가능한 그래프 없이 진단을 반환합니다.
    /// </summary>
    public interface IPackageResolver
    {
        /// <summary>
        /// Resolves all retained roots into a complete graph or blocking diagnostics.<br/>
        /// 유지할 전체 루트를 완전한 그래프로 해석하거나 진행을 차단하는 진단을 반환합니다.
        /// </summary>
        /// <param name="request">
        /// Retained roots, source overrides, and a search budget.<br/>
        /// 유지할 루트, 출처 재지정 및 탐색 예산입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns a validated graph or diagnostics without a graph.<br/>
        /// 완료되면 검증된 그래프 또는 그래프 없는 진단을 반환합니다.
        /// </returns>
        Task<PackageResult<ResolvedPackageGraph>> ResolveAsync(ResolutionRequest request, CancellationToken cancellationToken = default);
    }
}
