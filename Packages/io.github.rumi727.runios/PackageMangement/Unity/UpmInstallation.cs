#nullable enable
using System;
using System.Collections.Generic;
using UnityEditorInternal;
using System.IO;

namespace RuniOS.PackageManagement.Unity
{
    /// <summary>
    /// Contains a native package name, acquisition reference, and optional registry and assembly-definition requirements.<br/>
    /// native package 이름, 획득 참조와 선택적인 registry 및 어셈블리 정의 요구사항을 담습니다.
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
        /// Gets optional required scoped-registry configuration.<br/>
        /// 선택적인 필수 scoped registry 설정을 가져옵니다.
        /// </summary>
        public ScopedRegistryDefinition? registry { get; }
        /// <summary>
        /// Gets whether the executor must ensure the package itself in addition to registry configuration.<br/>
        /// registry 설정과 함께 executor가 package 자체를 ensure해야 하는지 여부를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// When <see langword="false"/>, only registry preparation and required assembly-definition assets are ensured; package acquisition is delegated to UPM and is not verified.<br/>
        /// <see langword="false"/>이면 registry 준비와 필수 어셈블리 정의 에셋만 ensure하며 package 획득은 UPM에 위임하고 검증하지 않습니다.
        /// </remarks>
        public bool ensurePackage { get; }
        /// <summary>
        /// Gets required assembly-definition assets, preserving missing reference slots.<br/>
        /// 누락된 참조 슬롯을 유지하며 필수 어셈블리 정의 에셋을 가져옵니다.
        /// </summary>
        public IReadOnlyList<AssemblyDefinitionAsset?> requiredAssemblyReferences { get; }
        internal string? requestedVersion { get; private set; }
        internal bool isGitReference { get; private set; }
        internal string? requestedGitRevision { get; private set; }
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
        /// <param name="registry">
        /// Optional scoped-registry requirements.<br/>
        /// 선택적인 scoped registry 요구사항입니다.
        /// </param>
        /// <param name="ensurePackage">
        /// Whether to ensure the package itself; <see langword="false"/> requires registry preparation and required assembly-definition assets.<br/>
        /// package 자체를 ensure할지 여부이며 <see langword="false"/>이면 registry 준비와 필수 어셈블리 정의 에셋을 요구합니다.
        /// </param>
        /// <param name="requiredAssemblyReferences">
        /// Required assembly-definition assets; <see langword="null"/> requires none, and missing entries are unmet requirements.<br/>
        /// 필수 어셈블리 정의 에셋이며 <see langword="null"/>이면 요구하지 않고 누락된 항목은 미충족 요구사항입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when a name or reference is empty.<br/>
        /// 이름 또는 참조가 비어 있으면 발생합니다.
        /// </exception>
        public UpmInstallation(string packageName, string packageReference, ScopedRegistryDefinition? registry = null, bool ensurePackage = true,
            IEnumerable<AssemblyDefinitionAsset?>? requiredAssemblyReferences = null)
        {
            if (string.IsNullOrWhiteSpace(packageName)) throw new ArgumentException("A native package name is required.", nameof(packageName));
            if (string.IsNullOrWhiteSpace(packageReference)) throw new ArgumentException("A UPM reference is required.", nameof(packageReference));
            this.packageName = packageName;
            this.packageReference = packageReference;
            this.registry = registry;
            this.ensurePackage = ensurePackage;
            this.requiredAssemblyReferences = requiredAssemblyReferences is null
                ? Array.Empty<AssemblyDefinitionAsset?>() : new List<AssemblyDefinitionAsset?>(requiredAssemblyReferences).AsReadOnly();
        }
        /// <summary>
        /// Creates a Git acquisition reference at the requested revision.<br/>
        /// 지정한 revision의 Git 획득 참조를 생성합니다.
        /// </summary>
        /// <param name="packageName">
        /// The native package name.<br/>
        /// native package 이름입니다.
        /// </param>
        /// <param name="repositoryUrl">
        /// The UPM-compatible Git URL without a fragment.<br/>
        /// fragment가 없는 UPM 호환 Git URL입니다.
        /// </param>
        /// <param name="revision">
        /// An optional tag, branch, or full commit hash; <see langword="null"/> or an empty string lets UPM use the default branch head.<br/>
        /// 선택적인 태그, 브랜치 또는 전체 commit hash이며 <see langword="null"/>이나 빈 문자열이면 UPM이 기본 브랜치의 최신 커밋을 사용합니다.
        /// </param>
        /// <param name="packagePath">
        /// The optional package path inside the repository.<br/>
        /// 저장소 안의 선택적인 package 경로입니다.
        /// </param>
        /// <param name="requiredAssemblyReferences">
        /// Required assembly-definition assets; <see langword="null"/> requires none, and missing entries are unmet requirements.<br/>
        /// 필수 어셈블리 정의 에셋이며 <see langword="null"/>이면 요구하지 않고 누락된 항목은 미충족 요구사항입니다.
        /// </param>
        /// <returns>
        /// The Git reference passed to UPM.<br/>
        /// UPM에 전달할 Git 참조를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the repository URL is invalid.<br/>
        /// 저장소 URL이 유효하지 않으면 발생합니다.
        /// </exception>
        public static UpmInstallation Git(string packageName, string repositoryUrl, string? revision = null, string? packagePath = null,
            IEnumerable<AssemblyDefinitionAsset?>? requiredAssemblyReferences = null)
        {
            if (string.IsNullOrWhiteSpace(repositoryUrl) || repositoryUrl.Contains("#")) throw new ArgumentException("A Git URL without a fragment is required.", nameof(repositoryUrl));
            string reference = repositoryUrl;
            if (!string.IsNullOrEmpty(packagePath))reference += (reference.Contains("?") ? "&" : "?") + "path=" + Uri.EscapeDataString("/" + packagePath!.TrimStart('/'));
            if (!string.IsNullOrEmpty(revision)) reference += "#" + revision;
            return new UpmInstallation(packageName, reference, requiredAssemblyReferences: requiredAssemblyReferences)
            {
                isGitReference = true,
                requestedGitRevision = string.IsNullOrEmpty(revision) ? null : revision
            };
        }
        /// <summary>
        /// Creates an exact-version registry acquisition reference without version solving.<br/>
        /// 버전 탐색 없이 exact version registry 획득 참조를 생성합니다.
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
        /// <param name="ensurePackage">
        /// Whether to ensure this native package or delegate acquisition to UPM dependency resolution.<br/>
        /// native package를 ensure할지 UPM dependency resolution에 획득을 위임할지 여부입니다.
        /// </param>
        /// <param name="requiredAssemblyReferences">
        /// Required assembly-definition assets; <see langword="null"/> requires none, and missing entries are unmet requirements.<br/>
        /// 필수 어셈블리 정의 에셋이며 <see langword="null"/>이면 요구하지 않고 누락된 항목은 미충족 요구사항입니다.
        /// </param>
        /// <returns>
        /// Registry configuration and an exact-version acquisition reference for an absent package name.<br/>
        /// registry 설정과 package 이름이 없을 때 사용하는 exact version 획득 참조를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the name or version is empty.<br/>
        /// 이름 또는 버전이 비어 있으면 발생합니다.
        /// </exception>
        public static UpmInstallation Registry(string packageName, string version, ScopedRegistryDefinition? registry = null, bool ensurePackage = true,
            IEnumerable<AssemblyDefinitionAsset?>? requiredAssemblyReferences = null)
        {
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("An exact version is required.", nameof(version));
            return new UpmInstallation(packageName, packageName + "@" + version, registry, ensurePackage, requiredAssemblyReferences)
            {
                requestedVersion = version
            };
        }
        /// <summary>
        /// Creates a local-directory acquisition reference for an absent package name.<br/>
        /// package 이름이 없을 때 사용하는 local directory 획득 참조를 생성합니다.
        /// </summary>
        /// <param name="packageName">
        /// The native package name.<br/>
        /// native package 이름입니다.
        /// </param>
        /// <param name="fullPath">
        /// The package directory path.<br/>
        /// package directory 경로입니다.
        /// </param>
        /// <param name="requiredAssemblyReferences">
        /// Required assembly-definition assets; <see langword="null"/> requires none, and missing entries are unmet requirements.<br/>
        /// 필수 어셈블리 정의 에셋이며 <see langword="null"/>이면 요구하지 않고 누락된 항목은 미충족 요구사항입니다.
        /// </param>
        /// <returns>
        /// Requirements for the designated local directory, without content verification.<br/>
        /// 내용 검증 없이 지정한 local directory의 요구사항을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when a name or path is invalid.<br/>
        /// 이름 또는 경로가 유효하지 않으면 발생합니다.
        /// </exception>
        public static UpmInstallation Local(string packageName, string fullPath, IEnumerable<AssemblyDefinitionAsset?>? requiredAssemblyReferences = null)
        {
            string path = Path.GetFullPath(fullPath);
            return new UpmInstallation(packageName, "file:" + path.Replace('\\', '/'), requiredAssemblyReferences: requiredAssemblyReferences);
        }
    }
}
