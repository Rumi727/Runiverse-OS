#nullable enable
using System;
using System.Collections.Generic;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Associates a dependency declaration with its owning package metadata.<br/>
    /// 의존성 선언을 소유 패키지의 메타데이터와 연결합니다.
    /// </summary>
    public sealed class PackageDependency
    {
        /// <summary>
        /// Gets the original dependency requirement.<br/>
        /// 원본 의존성 요구를 가져옵니다.
        /// </summary>
        public PackageRequirement requirement { get; }

        /// <summary>
        /// Retains an immutable dependency declaration.<br/>
        /// 불변 의존성 선언을 보존합니다.
        /// </summary>
        /// <param name="requirement">
        /// The immutable dependency declaration.<br/>
        /// 불변 의존성 선언입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public PackageDependency(PackageRequirement requirement) =>
            this.requirement = requirement ?? throw new ArgumentNullException(nameof(requirement));
    }
}
