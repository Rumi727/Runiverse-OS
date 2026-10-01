#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Internal;
using RuniOS.PackageManagement.Unity.Editor.Registry;
using RuniOS.PackageManagement.Unity.Editor.Builtin;

namespace RuniOS.PackageManagement.Unity.Editor.Metadata
{
    /// <summary>
    /// Routes ordinary UPM dependencies using longest matching registry scopes.<br/>
    /// 가장 긴 레지스트리 범위로 일반 UPM 의존성을 연결합니다.
    /// </summary>
    public sealed class UpmSourceConfiguration
    {
        /// <summary>
        /// Gets the fallback registry endpoint.<br/>
        /// 기본 레지스트리 주소를 가져옵니다.
        /// </summary>
        public string defaultRegistryUrl { get; }
        /// <summary>
        /// Gets the immutable scoped-registry routing definitions.<br/>
        /// 불변 scoped registry 연결 정의를 가져옵니다.
        /// </summary>
        public IReadOnlyList<ScopedRegistryDefinition> registries { get; }
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="registries">
        /// Optional scoped-registry routing definitions.<br/>
        /// 선택적인 scoped registry 연결 정의입니다.
        /// </param>
        /// <param name="defaultRegistryUrl">
        /// The registry fallback for names without a matching scope.<br/>
        /// 일치하는 범위가 없는 이름에 사용할 기본 레지스트리입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public UpmSourceConfiguration(IEnumerable<ScopedRegistryDefinition>? registries = null, string defaultRegistryUrl = "https://packages.unity.com")
        {
            this.defaultRegistryUrl = AdapterData.HttpUrl(defaultRegistryUrl);
            this.registries = AdapterData.List(registries ?? Array.Empty<ScopedRegistryDefinition>());
            if (this.registries.Any(x => x is null)) throw new ArgumentException("Null registry definitions are not allowed.", nameof(registries));
        }
        /// <summary>
        /// Selects the longest matching registry scope, rejecting ambiguous endpoints.<br/>
        /// 가장 긴 registry 범위를 선택하고 주소가 모호하면 거부합니다.
        /// </summary>
        /// <param name="id">
        /// The logical package identifier.<br/>
        /// 논리적 패키지 식별자입니다.
        /// </param>
        /// <returns>
        /// The scoped or fallback registry source.<br/>
        /// scoped registry 또는 기본 registry 출처입니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public RegistryPackageSource RegistrySource(PackageId id)
        {
            int length = registries.Select(x => x.Match(id)).DefaultIfEmpty(-1).Max();
            ScopedRegistryDefinition[] matches = registries.Where(x => x.Match(id) == length && length >= 0).ToArray();
            if (matches.Select(x => x.url).Distinct(StringComparer.Ordinal).Count() > 1)
                throw new ArgumentException($"Registry scopes for '{id}' are ambiguous.");
            ScopedRegistryDefinition? registry = matches.FirstOrDefault();
            return new RegistryPackageSource(id, registry?.url ?? defaultRegistryUrl, registry);
        }
        /// <summary>
        /// Creates an exact-version requirement for a registry or built-in module.<br/>
        /// registry 또는 내장 모듈의 정확한 버전 요구사항을 생성합니다.
        /// </summary>
        /// <param name="id">
        /// The logical package identifier.<br/>
        /// 논리적 패키지 식별자입니다.
        /// </param>
        /// <param name="version">
        /// The exact UPM version, excluding version ranges.<br/>
        /// 버전 범위를 제외한 정확한 UPM 버전입니다.
        /// </param>
        /// <param name="location">
        /// Optional declaration provenance.<br/>
        /// 선택적인 선언 위치 정보입니다.
        /// </param>
        /// <returns>
        /// The declaration bound to the selected source.<br/>
        /// 선택한 출처에 연결된 선언입니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public PackageRequirement VersionRequirement(PackageId id, string version, DeclarationLocation? location = null)
        {
            PackageSource source = id.value.StartsWith("com.unity.modules.", StringComparison.Ordinal) ?
                new UnityBuiltinSource(id) : RegistrySource(id);
            return new PackageRequirement(id, source, new[] { new UpmVersionConstraint(version) }, location);
        }
    }
}
