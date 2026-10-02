#nullable enable
using System;
using System.IO;
using UnityEditor.PackageManager;

namespace RuniOS.PackageManagement.Unity
{
    /// <summary>
    /// Contains a native UPM reference and the condition that satisfies this installation.<br/>
    /// native UPM 참조와 이 설치를 만족시키는 조건을 담습니다.
    /// </summary>
    public sealed class UpmInstallation : IInstallation
    {
        /// <summary>
        /// Gets the native package name used for observation and collision checks.<br/>
        /// 관측과 충돌 확인에 사용하는 native package 이름을 가져옵니다.
        /// </summary>
        public string packageName { get; }
        /// <summary>
        /// Gets the complete reference passed to UPM.<br/>
        /// UPM에 전달하는 완결된 참조를 가져옵니다.
        /// </summary>
        public string packageReference { get; }
        /// <summary>
        /// Gets the condition evaluated against native package information.<br/>
        /// native package 정보로 평가하는 조건을 가져옵니다.
        /// </summary>
        public Func<PackageInfo, bool> isSatisfied { get; }
        /// <summary>
        /// Gets optional required scoped-registry configuration.<br/>
        /// 선택적인 필수 scoped registry 설정을 가져옵니다.
        /// </summary>
        public ScopedRegistryDefinition? registry { get; }
        /// <summary>
        /// Creates complete UPM requirements without binding an executor.<br/>
        /// executor를 결합하지 않고 완결된 UPM 요구사항을 생성합니다.
        /// </summary>
        /// <param name="packageName">
        /// The native package name.<br/>
        /// native package 이름입니다.
        /// </param>
        /// <param name="packageReference">
        /// The exact native UPM request reference.<br/>
        /// 정확한 native UPM 요청 참조입니다.
        /// </param>
        /// <param name="isSatisfied">
        /// The native observation predicate.<br/>
        /// native 관측 predicate입니다.
        /// </param>
        /// <param name="registry">
        /// Optional scoped-registry requirements.<br/>
        /// 선택적인 scoped registry 요구사항입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when a name or reference is empty.<br/>
        /// 이름 또는 참조가 비어 있으면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when isSatisfied is null.<br/>
        /// isSatisfied가 null이면 발생합니다.
        /// </exception>
        public UpmInstallation(string packageName, string packageReference, Func<PackageInfo, bool> isSatisfied, ScopedRegistryDefinition? registry = null)
        {
            if (string.IsNullOrWhiteSpace(packageName)) throw new ArgumentException("A native package name is required.", nameof(packageName));
            if (string.IsNullOrWhiteSpace(packageReference)) throw new ArgumentException("A UPM reference is required.", nameof(packageReference));
            this.packageName = packageName;
            this.packageReference = packageReference;
            this.isSatisfied = isSatisfied ?? throw new ArgumentNullException(nameof(isSatisfied));
            this.registry = registry;
        }
        /// <summary>
        /// Creates Git requirements pinned to a full commit hash.<br/>
        /// 전체 commit hash로 고정한 Git 요구사항을 생성합니다.
        /// </summary>
        /// <param name="packageName">
        /// The native package name.<br/>
        /// native package 이름입니다.
        /// </param>
        /// <param name="repositoryUrl">
        /// The UPM-compatible Git URL without a fragment.<br/>
        /// fragment가 없는 UPM 호환 Git URL입니다.
        /// </param>
        /// <param name="commit">
        /// The full 40-character commit hash.<br/>
        /// 40자 전체 commit hash입니다.
        /// </param>
        /// <param name="packagePath">
        /// The optional package path inside the repository.<br/>
        /// 저장소 안의 선택적인 package 경로입니다.
        /// </param>
        /// <returns>
        /// Requirements verified by the resolved Git commit hash.<br/>
        /// 해석된 Git commit hash로 확인하는 요구사항을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the Git reference or commit is invalid.<br/>
        /// Git 참조 또는 commit이 유효하지 않으면 발생합니다.
        /// </exception>
        public static UpmInstallation Git(string packageName, string repositoryUrl, string commit, string? packagePath = null)
        {
            if (string.IsNullOrWhiteSpace(repositoryUrl) || repositoryUrl.Contains("#")) throw new ArgumentException("A Git URL without a fragment is required.", nameof(repositoryUrl));
            if (commit is null || commit.Length != 40) throw new ArgumentException("A full 40-character commit hash is required.", nameof(commit));
            foreach (char character in commit)
                if (!Uri.IsHexDigit(character)) throw new ArgumentException("The commit hash must be hexadecimal.", nameof(commit));
            string pin = commit.ToLowerInvariant();
            string reference = repositoryUrl;
            if (!string.IsNullOrEmpty(packagePath))reference += (reference.Contains("?") ? "&" : "?") + "path=" + Uri.EscapeDataString("/" + packagePath!.TrimStart('/'));
            reference += "#" + pin;
            return new UpmInstallation(packageName, reference, info => info.source == PackageSource.Git && info.git is not null && StringComparer.OrdinalIgnoreCase.Equals(info.git.hash, pin));
        }
        /// <summary>
        /// Creates exact-version registry requirements without version solving.<br/>
        /// 버전 탐색 없이 exact version registry 요구사항을 생성합니다.
        /// </summary>
        /// <param name="packageName">
        /// The native package name.<br/>
        /// native package 이름입니다.
        /// </param>
        /// <param name="version">
        /// The exact version string supplied by the author.<br/>
        /// 작성자가 제공한 exact version 문자열입니다.
        /// </param>
        /// <param name="registry">
        /// Optional required scoped-registry configuration.<br/>
        /// 선택적인 필수 scoped registry 설정입니다.
        /// </param>
        /// <returns>
        /// Requirements verified by native source and exact version.<br/>
        /// native source와 exact version으로 확인하는 요구사항을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the name or version is empty.<br/>
        /// 이름 또는 버전이 비어 있으면 발생합니다.
        /// </exception>
        public static UpmInstallation Registry(string packageName, string version, ScopedRegistryDefinition? registry = null)
        {
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("An exact version is required.", nameof(version));
            return new UpmInstallation(packageName, packageName + "@" + version, info =>
                (info.source == PackageSource.Registry || (registry is null && info.source == PackageSource.BuiltIn)) &&
                StringComparer.Ordinal.Equals(info.version, version) &&
                (registry is null || (info.registry is not null && StringComparer.Ordinal.Equals(info.registry.url.TrimEnd('/'), registry.url))), registry);
        }
        /// <summary>
        /// Creates local-directory requirements verified by the resolved path.<br/>
        /// 해석된 경로로 확인하는 local directory 요구사항을 생성합니다.
        /// </summary>
        /// <param name="packageName">
        /// The native package name.<br/>
        /// native package 이름입니다.
        /// </param>
        /// <param name="fullPath">
        /// The package directory path.<br/>
        /// package directory 경로입니다.
        /// </param>
        /// <returns>
        /// Requirements for the designated local directory, without content verification.<br/>
        /// 내용 검증 없이 지정한 local directory의 요구사항을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when a name or path is invalid.<br/>
        /// 이름 또는 경로가 유효하지 않으면 발생합니다.
        /// </exception>
        public static UpmInstallation Local(string packageName, string fullPath)
        {
            string path = Path.GetFullPath(fullPath);
            StringComparison comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return new UpmInstallation(packageName, "file:" + path.Replace('\\', '/'), info => info.source == PackageSource.Local &&
                string.Equals(Path.GetFullPath(info.resolvedPath).TrimEnd('/', '\\'), path.TrimEnd('/', '\\'), comparison));
        }
    }
}
