#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Planning
{
    /// <summary>
    /// Declares abstract resource access for generic conflict checks.<br/>
    /// 공통 충돌 검사에 사용할 추상 리소스 접근을 선언합니다.
    /// </summary>
    public sealed class ResourceClaim
    {
        /// <summary>
        /// Gets the canonical opaque resource identifier used for equality-based conflict checks.<br/>
        /// 동등성 기반 충돌 검사에 사용할 정규화된 불투명 리소스 식별자를 가져옵니다.
        /// </summary>
        public string resource { get; }
        /// <summary>
        /// Gets the declared read or write access to the resource.<br/>
        /// 선언된 리소스 읽기 또는 쓰기 접근을 가져옵니다.
        /// </summary>
        public ResourceAccess access { get; }

        /// <summary>
        /// Captures resource access without interpreting resource semantics.<br/>
        /// 리소스 의미를 해석하지 않고 리소스 접근을 저장합니다.
        /// </summary>
        /// <param name="resource">
        /// The canonical opaque resource key.<br/>
        /// 정규화된 불투명 리소스 키입니다.
        /// </param>
        /// <param name="access">
        /// The declared read or write access.<br/>
        /// 선언된 읽기 또는 쓰기 접근입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="resource"/> is empty or whitespace.<br/>
        /// <paramref name="resource"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a numeric or enumeration argument is outside its accepted range.<br/>
        /// 숫자 또는 열거형 인수가 허용 범위를 벗어나면 발생합니다.
        /// </exception>
        public ResourceClaim(string resource, ResourceAccess access)
        {
            this.resource = Snapshots.Text(resource, nameof(resource));
            if (access != ResourceAccess.Read && access != ResourceAccess.Write)
                throw new ArgumentOutOfRangeException(nameof(access));
            this.access = access;
        }
    }
}
