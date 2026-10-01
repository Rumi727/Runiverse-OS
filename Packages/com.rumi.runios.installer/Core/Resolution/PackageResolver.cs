#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Providers;
using RuniOS.PackageManagement.Resolution.Internal;

namespace RuniOS.PackageManagement.Resolution
{
    /// <summary>
    /// Searches candidate combinations and publishes only complete, compatible, acyclic graphs.<br/>
    /// 후보 조합을 탐색하고 완전하고 호환되며 순환 없는 그래프만 제공합니다.
    /// </summary>
    public sealed class PackageResolver : IPackageResolver
    {
        readonly PackageManagementServices services;

        internal PackageResolver(PackageManagementServices services) => this.services = services;

        /// <summary>
        /// Resolves the complete dependency closure, revisiting candidates when later requirements conflict.<br/>
        /// 나중에 추가된 요구가 충돌하면 후보를 다시 탐색하여 전체 의존성 집합을 해석합니다.
        /// </summary>
        /// <param name="request">
        /// All retained root requirements, source overrides, and the search budget.<br/>
        /// 유지할 전체 루트 요구, 출처 재지정 및 탐색 예산입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns a validated graph or diagnostics without a graph.<br/>
        /// 완료되면 검증된 그래프를 반환하거나 그래프 없이 진단을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="request"/> is <see langword="null"/>.<br/>
        /// <paramref name="request"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when cancellation is requested.<br/>
        /// 취소가 요청되면 발생합니다.
        /// </exception>
        public Task<PackageResult<ResolvedPackageGraph>> ResolveAsync(ResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            return new Search(services, request, cancellationToken).RunAsync(new Dictionary<PackageId, Selection>());
        }
    }
}
