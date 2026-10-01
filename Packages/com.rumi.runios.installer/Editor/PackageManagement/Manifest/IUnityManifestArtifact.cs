#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Registry;

namespace RuniOS.PackageManagement.Unity.Editor.Manifest
{
    /// <summary>
    /// Exposes a passive UPM manifest representation independently of the acquisition source.<br/>
    /// 획득 출처와 독립적으로 수동적인 UPM manifest 표현을 제공합니다.
    /// </summary>
    public interface IUnityManifestArtifact
    {
        /// <summary>
        /// Gets the exact value to place in project manifest dependencies.<br/>
        /// 프로젝트 manifest 의존성에 기록할 정확한 값을 가져옵니다.
        /// </summary>
        string dependencyValue { get; }
        /// <summary>
        /// Gets the UPM request identifier for installing this representation.<br/>
        /// 이 표현을 설치할 UPM 요청 식별자를 가져옵니다.
        /// </summary>
        string packageIdentifier { get; }
        /// <summary>
        /// Maps the representation to a native UPM identity without inspecting an environment.<br/>
        /// 환경을 조회하지 않고 표현을 UPM 고유 식별자에 대응시킵니다.
        /// </summary>
        /// <param name="id">
        /// The logical package identifier.<br/>
        /// 논리적 패키지 식별자입니다.
        /// </param>
        /// <returns>
        /// The native UPM identity expressed by this artifact.<br/>
        /// 이 artifact가 표현하는 UPM 고유 식별자입니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        PackageIdentity GetInstalledIdentity(PackageId id);
        /// <summary>
        /// Gets required scoped-registry configuration, or <see langword="null"/> when none is required.<br/>
        /// 필요한 scoped registry 설정을 가져오며 필요하지 않으면 <see langword="null"/>을 반환합니다.
        /// </summary>
        ScopedRegistryDefinition? registry { get; }
    }
}
