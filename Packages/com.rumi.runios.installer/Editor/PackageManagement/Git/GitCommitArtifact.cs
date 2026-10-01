#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Internal;
using RuniOS.PackageManagement.Unity.Editor.Manifest;
using RuniOS.PackageManagement.Unity.Editor.Registry;

namespace RuniOS.PackageManagement.Unity.Editor.Git
{
    /// <summary>
    /// Captures a package location at an exact commit and the symbolic references resolved to it.<br/>
    /// 정확한 commit의 패키지 위치와 해당 commit으로 해석된 symbolic reference를 저장합니다.
    /// </summary>
    public sealed class GitCommitArtifact : PackageArtifactReference, IUnityManifestArtifact
    {
        /// <summary>
        /// Identifies the capability routing key.<br/>
        /// 기능 연결 키를 식별합니다.
        /// </summary>
        public const string artifactKind = "upm:git-commit";
        /// <summary>
        /// Gets the immutable acquisition-source descriptor.<br/>
        /// 불변 획득 출처 설명자를 가져옵니다.
        /// </summary>
        public GitPackageSource source { get; }
        /// <summary>
        /// Gets the complete lowercase Git commit hash.<br/>
        /// 소문자로 된 전체 Git commit hash를 가져옵니다.
        /// </summary>
        public string commit { get; }
        /// <summary>
        /// Gets immutable symbolic references resolved to this commit.<br/>
        /// 이 commit으로 해석된 불변 symbolic reference를 가져옵니다.
        /// </summary>
        public IReadOnlyList<string> references { get; }
        /// <inheritdoc/>
        public ScopedRegistryDefinition? registry => null;
        /// <inheritdoc/>
        public string dependencyValue => "git+" + source.repositoryUrl + (source.packagePath.Length == 0 ? "" : "?path=/" + string.Join("/", source.packagePath.Split('/').Select(Uri.EscapeDataString))) + "#" + commit;
        /// <inheritdoc/>
        public string packageIdentifier => dependencyValue;
        /// <inheritdoc/>
        public PackageIdentity GetInstalledIdentity(PackageId id) => new(id, GitPackageSource.sourceKind, source.key, commit);
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="source">
        /// The immutable acquisition-source descriptor.<br/>
        /// 불변 획득 출처 설명자입니다.
        /// </param>
        /// <param name="commit">
        /// The complete lowercase Git commit hash.<br/>
        /// 소문자로 된 전체 Git commit hash입니다.
        /// </param>
        /// <param name="references">
        /// Optional symbolic references resolved to this commit.<br/>
        /// 이 commit으로 해석된 선택적인 symbolic reference입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public GitCommitArtifact(GitPackageSource source, string commit, IEnumerable<string>? references = null) : base(artifactKind)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            if (!System.Text.RegularExpressions.Regex.IsMatch(commit ?? "", @"^(?:[0-9a-f]{40}|[0-9a-f]{64})$"))
                throw new ArgumentException("A full lowercase Git commit hash is required.", nameof(commit));
            this.commit = commit; this.references = AdapterData.List(references ?? Array.Empty<string>());
        }
    }
}
