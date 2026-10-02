#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement.Unity
{
    /// <summary>
    /// Describes scoped-registry configuration required by a UPM request.<br/>
    /// UPM 요청에 필요한 scoped registry 설정을 표현합니다.
    /// </summary>
    public sealed class ScopedRegistryDefinition
    {
        /// <summary>
        /// Gets the registry display name.<br/>
        /// registry 표시 이름을 가져옵니다.
        /// </summary>
        public string name { get; }
        /// <summary>
        /// Gets the absolute registry URL.<br/>
        /// 절대 registry URL을 가져옵니다.
        /// </summary>
        public string url { get; }
        /// <summary>
        /// Gets the package scopes served by the registry.<br/>
        /// registry가 제공하는 package scope를 가져옵니다.
        /// </summary>
        public IReadOnlyList<string> scopes { get; }
        /// <summary>
        /// Creates required registry configuration.<br/>
        /// 필요한 registry 설정을 생성합니다.
        /// </summary>
        /// <param name="name">
        /// The display name.<br/>
        /// 표시 이름입니다.
        /// </param>
        /// <param name="url">
        /// The absolute HTTP or HTTPS URL.<br/>
        /// 절대 HTTP 또는 HTTPS URL입니다.
        /// </param>
        /// <param name="scopes">
        /// Non-empty package scopes.<br/>
        /// 비어 있지 않은 package scope들입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when configuration is invalid.<br/>
        /// 설정이 유효하지 않으면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when scopes is null.<br/>
        /// scopes가 null이면 발생합니다.
        /// </exception>
        public ScopedRegistryDefinition(string name, string url, IEnumerable<string> scopes)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A registry name is required.", nameof(name));
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri is null || (uri.Scheme != "http" && uri.Scheme != "https"))
                throw new ArgumentException("An absolute HTTP registry URL is required.", nameof(url));
            string[] values = (scopes ?? throw new ArgumentNullException(nameof(scopes))).Distinct(StringComparer.Ordinal).ToArray();
            if (values.Length == 0 || values.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Non-empty registry scopes are required.", nameof(scopes));
            this.name = name;
            this.url = url.TrimEnd('/');
            this.scopes = Array.AsReadOnly(values);
        }
    }
}
