#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Unity.Editor.Metadata
{
    /// <summary>
    /// Decodes one sidecar source declaration without changes to the central reader.<br/>
    /// 중앙 리더 변경 없이 sidecar 출처 선언 하나를 해석합니다.
    /// </summary>
    public interface IUpmDependencyDeclarationReader
    {
        /// <summary>
        /// Gets the sidecar source key handled by this reader.<br/>
        /// 이 리더가 처리할 sidecar 출처 키를 가져옵니다.
        /// </summary>
        string sourceKind { get; }
        /// <summary>
        /// Decodes immutable dependency metadata and reports invalid declarations.<br/>
        /// 불변 의존성 메타데이터를 해석하고 잘못된 선언을 보고합니다.
        /// </summary>
        /// <param name="id">
        /// The logical package identifier.<br/>
        /// 논리적 패키지 식별자입니다.
        /// </param>
        /// <param name="declarationJson">
        /// The sidecar source declaration as JSON.<br/>
        /// JSON으로 표현한 sidecar 출처 선언입니다.
        /// </param>
        /// <param name="context">
        /// The parent identity, registry routes and declaration provenance.<br/>
        /// 부모 식별자, 레지스트리 연결 및 선언 위치 정보입니다.
        /// </param>
        /// <returns>
        /// The declaration bound to the selected source.<br/>
        /// 선택한 출처에 연결된 선언입니다.
        /// </returns>
        /// <exception cref="FormatException">
        /// May be thrown when the JSON declaration is malformed or incomplete.<br/>
        /// JSON 선언이 잘못되거나 불완전하면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when a source or constraint value is invalid.<br/>
        /// 출처 또는 제약 값이 잘못되면 발생할 수 있습니다.
        /// </exception>
        PackageRequirement Read(PackageId id, string declarationJson, UpmDeclarationContext context);
    }
}
