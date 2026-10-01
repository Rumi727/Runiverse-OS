#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Internal;

namespace RuniOS.PackageManagement.Unity.Editor.Registry
{
    /// <summary>
    /// Defines a registry endpoint and package-name scopes without credentials.<br/>
    /// 인증 정보 없이 레지스트리 주소와 패키지 이름 범위를 정의합니다.
    /// </summary>
    public sealed class ScopedRegistryDefinition
    {
        /// <summary>
        /// Gets the registry display name.<br/>
        /// 레지스트리 표시 이름을 가져옵니다.
        /// </summary>
        public string name { get; }
        /// <summary>
        /// Gets the canonical registry endpoint.<br/>
        /// 정규화된 레지스트리 주소를 가져옵니다.
        /// </summary>
        public string url { get; }
        /// <summary>
        /// Gets the immutable package-name scopes.<br/>
        /// 불변 패키지 이름 범위를 가져옵니다.
        /// </summary>
        public IReadOnlyList<string> scopes { get; }
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="name">
        /// The registry display name.<br/>
        /// 레지스트리 표시 이름입니다.
        /// </param>
        /// <param name="url">
        /// The credential-free registry HTTP(S) endpoint.<br/>
        /// 인증 정보를 포함하지 않는 registry HTTP(S) 주소입니다.
        /// </param>
        /// <param name="scopes">
        /// The package-name scopes to route.<br/>
        /// 연결할 패키지 이름 범위입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public ScopedRegistryDefinition(string name, string url, IEnumerable<string> scopes)
        {
            this.name = AdapterData.Text(name, nameof(name)); this.url = AdapterData.HttpUrl(url);
            this.scopes = AdapterData.List((scopes ?? throw new ArgumentNullException(nameof(scopes))).Select(AdapterData.PackageName).Distinct(StringComparer.Ordinal));
            if (this.scopes.Count == 0) throw new ArgumentException("At least one scope is required.", nameof(scopes));
        }
        internal int Match(PackageId id) => scopes.Where(scope => id.value == scope || id.value.StartsWith(scope + ".", StringComparison.Ordinal))
            .Select(scope => scope.Length).DefaultIfEmpty(-1).Max();
    }
}
