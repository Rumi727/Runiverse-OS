#nullable enable
using UnityEngine;
using UnityEngine.Serialization;

namespace RuniOS.PackageManagement.Unity
{
    /// <summary>
    /// Defines a package at a Git revision with authored direct dependencies.<br/>
    /// 작성한 직접 dependencies와 Git revision으로 package를 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "GitPackage", menuName = "Runiverse OS/Installer/Git Package")]
    public sealed class GitPackage : PackageAsset
    {
        /// <summary>
        /// Gets or sets the repository URL without a fragment.<br/>
        /// fragment가 없는 저장소 URL을 가져오거나 설정합니다.
        /// </summary>
        public string repositoryUrl = string.Empty;
        /// <summary>
        /// Gets or sets an optional Git tag, branch, or full commit hash.<br/>
        /// 선택적인 Git 태그, 브랜치 또는 전체 commit hash를 가져오거나 설정합니다.
        /// </summary>
        [FormerlySerializedAs("commit")]
        public string revision = string.Empty;
        /// <summary>
        /// Gets or sets the optional repository package path.<br/>
        /// 저장소 안의 선택적인 package 경로를 가져오거나 설정합니다.
        /// </summary>
        public string packagePath = string.Empty;
        /// <inheritdoc/>
        public override string exactIdentity => CreateUpmInstallation().packageReference;
        /// <inheritdoc/>
        public override IInstallation CreateInstallation(bool isRoot) => CreateUpmInstallation();
        UpmInstallation CreateUpmInstallation() => UpmInstallation.Git(nativeName, repositoryUrl, revision, packagePath, requiredAssemblyReferences);
    }
}
