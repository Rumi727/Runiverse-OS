#nullable enable
using System;
using System.Collections.Generic;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Describes a canonical source location; subclasses must keep all payloads immutable.<br/>
    /// 정규화된 출처 위치를 나타내며 파생 클래스는 모든 데이터를 불변으로 유지해야 합니다.
    /// </summary>
    public abstract class PackageSource
    {
        /// <summary>
        /// Gets the routing key or transition category consumed by the corresponding provider.<br/>
        /// 해당 프로바이더가 사용하는 라우팅 키 또는 전이 분류를 가져옵니다.
        /// </summary>
        public string kind { get; }
        /// <summary>
        /// Gets the canonical location or configuration key used for descriptor comparison.<br/>
        /// 설명자 비교에 사용하는 정규화된 위치 또는 설정 키를 가져옵니다.
        /// </summary>
        public string key { get; }

        /// <summary>
        /// Captures canonical source location and routing information.<br/>
        /// 정규화된 출처 위치와 라우팅 정보를 저장합니다.
        /// </summary>
        /// <param name="kind">
        /// The namespaced capability routing key.<br/>
        /// 네임스페이스를 가진 기능 라우팅 키입니다.
        /// </param>
        /// <param name="key">
        /// The canonical descriptor identity within its kind.<br/>
        /// 해당 종류 내의 정규화된 설명자 식별자입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="kind"/> or <paramref name="key"/> is empty or whitespace.<br/>
        /// <paramref name="kind"/> 또는 <paramref name="key"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        protected PackageSource(string kind, string key)
        {
            this.kind = Snapshots.Text(kind, nameof(kind));
            this.key = Snapshots.Text(key, nameof(key));
        }
    }
}
