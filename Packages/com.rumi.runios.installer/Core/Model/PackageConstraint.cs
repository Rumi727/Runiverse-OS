#nullable enable
using System;
using System.Collections.Generic;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Describes a selection constraint interpreted by a registered evaluator.<br/>
    /// 등록된 평가기가 해석하는 선택 제약을 나타냅니다.
    /// </summary>
    public abstract class PackageConstraint
    {
        /// <summary>
        /// Gets the routing key or transition category consumed by the corresponding provider.<br/>
        /// 해당 프로바이더가 사용하는 라우팅 키 또는 전이 분류를 가져옵니다.
        /// </summary>
        public string kind { get; }

        /// <summary>
        /// Assigns selection semantics to a registered evaluator key.<br/>
        /// 선택 의미를 등록된 평가기 키에 할당합니다.
        /// </summary>
        /// <param name="kind">
        /// The namespaced capability routing key.<br/>
        /// 네임스페이스를 가진 기능 라우팅 키입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="kind"/> is empty or whitespace.<br/>
        /// <paramref name="kind"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        protected PackageConstraint(string kind) => this.kind = Snapshots.Text(kind, nameof(kind));
    }
}
