#nullable enable
using System.Collections.Generic;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Provides an exact package definition, direct dependencies, and installation requirements.<br/>
    /// exact 패키지 정의, 직접 의존성과 설치 요구사항을 제공합니다.
    /// </summary>
    /// <remarks>
    /// Definitions must remain stable while a graph is constructed and consumed.<br/>
    /// 정의는 graph를 구성하고 사용하는 동안 변경되지 않아야 합니다.
    /// </remarks>
    public interface IPackage
    {
        /// <summary>
        /// Gets the logical package identifier.<br/>
        /// 논리적 패키지 식별자를 가져옵니다.
        /// </summary>
        PackageId id { get; }
        /// <summary>
        /// Gets the opaque deterministic identity of this exact definition.<br/>
        /// 이 exact 정의의 불투명하고 결정적인 identity를 가져옵니다.
        /// </summary>
        string exactIdentity { get; }
        /// <summary>
        /// Gets direct references to dependencies; null entries represent missing references.<br/>
        /// 의존성의 직접 참조를 가져오며 <see langword="null"/> 항목은 누락된 참조를 나타냅니다.
        /// </summary>
        IReadOnlyList<IPackage?> dependencies { get; }
        /// <summary>
        /// Creates an installation descriptor without selecting an executor.<br/>
        /// executor를 선택하지 않고 설치 요구사항 descriptor를 생성합니다.
        /// </summary>
        /// <returns>
        /// A descriptor containing the complete installation requirements for this definition.<br/>
        /// 이 정의의 완결된 설치 요구사항을 담은 descriptor를 반환합니다.
        /// </returns>
        IInstallation CreateInstallation();
    }
}
