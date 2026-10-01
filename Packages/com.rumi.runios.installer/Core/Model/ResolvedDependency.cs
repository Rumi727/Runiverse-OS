#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Retains the declaration that connects two resolved packages.<br/>
    /// 해석된 두 패키지를 연결하는 선언을 보존합니다.
    /// </summary>
    public sealed class ResolvedDependency
    {
        /// <summary>
        /// Gets the package that declares this dependency edge.<br/>
        /// 이 의존성 간선을 선언한 패키지를 가져옵니다.
        /// </summary>
        public PackageId owner { get; }
        /// <summary>
        /// Gets the selected dependency package identifier.<br/>
        /// 선택된 의존성 패키지 식별자를 가져옵니다.
        /// </summary>
        public PackageId dependency => requirement.id;
        /// <summary>
        /// Gets the original dependency requirement.<br/>
        /// 원본 의존성 요구를 가져옵니다.
        /// </summary>
        public PackageRequirement requirement { get; }

        internal ResolvedDependency(PackageId owner, PackageRequirement requirement)
        {
            this.owner = owner;
            this.requirement = requirement;
        }
    }
}
