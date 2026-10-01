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
    /// Retrieves exact candidate identities and their dependency metadata for one source kind.<br/>
    /// 한 출처 종류의 정확한 후보 식별자와 의존성 메타데이터를 조회합니다.
    /// </summary>
    /// <remarks>
    /// Candidate identities must use the query's canonical source kind and key. Metadata must be complete for the candidate revision.<br/>
    /// 후보 식별자는 쿼리의 정규화된 출처 종류와 키를 사용해야 하며 메타데이터는 후보 리비전의 전체 정보를 포함해야 합니다.
    /// </remarks>
    public interface IPackageCatalogProvider
    {
        /// <summary>
        /// Gets the capability key for the canonical source.<br/>
        /// 정규화된 출처의 기능 키를 가져옵니다.
        /// </summary>
        string sourceKind { get; }

        /// <summary>
        /// Finds candidates in deterministic preference order without changing the installation target.<br/>
        /// 설치 대상을 변경하지 않고 결정적인 선호 순서로 후보를 검색합니다.
        /// </summary>
        /// <param name="query">
        /// The bound source and known requirements.<br/>
        /// 바인딩된 출처와 알려진 요구입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns candidate identities and diagnostics.<br/>
        /// 완료되면 후보 식별자와 진단을 반환합니다.
        /// </returns>
        Task<PackageResult<CandidateSet>> FindCandidatesAsync(PackageQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves declarations from the exact candidate revision.<br/>
        /// 정확한 후보 리비전에서 선언을 조회합니다.
        /// </summary>
        /// <param name="candidate">
        /// The candidate selected for metadata retrieval.<br/>
        /// 메타데이터를 조회할 후보입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns dependency metadata and diagnostics.<br/>
        /// 완료되면 의존성 메타데이터와 진단을 반환합니다.
        /// </returns>
        Task<PackageResult<PackageMetadata>> ReadMetadataAsync(PackageCandidate candidate, CancellationToken cancellationToken);
    }
}
