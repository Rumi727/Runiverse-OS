#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement.Resolution
{
    /// <summary>
    /// Identifies one resolution's lifetime for provider-owned snapshot caches.<br/>
    /// 프로바이더 소유 스냅샷 캐시를 위해 한 해석의 수명을 식별합니다.
    /// </summary>
    /// <remarks>
    /// Providers may use this object as a weak cache key. It does not contain services or mutable extension state.<br/>
    /// 프로바이더는 이 객체를 약한 캐시 키로 사용할 수 있으며 서비스나 가변 확장 상태를 포함하지 않습니다.
    /// </remarks>
    public sealed class ResolutionSession
    {
        /// <summary>
        /// Gets the unique identity of this resolution invocation.<br/>
        /// 이 해석 호출의 고유 식별자를 가져옵니다.
        /// </summary>
        public Guid id { get; } = Guid.NewGuid();

        internal ResolutionSession() { }
    }
}
