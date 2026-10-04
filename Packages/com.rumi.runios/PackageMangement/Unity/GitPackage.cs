#nullable enable
using UnityEngine;

namespace RuniOS.PackageManagement.Unity
{
    /// <summary>
    /// Defines a package pinned to a Git commit with authored direct dependencies.<br/>
    /// 작성한 직접 dependencies와 Git commit으로 고정한 package를 정의합니다.
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
        /// Gets or sets the full commit hash.<br/>
        /// 전체 commit hash를 가져오거나 설정합니다.
        /// </summary>
        public string commit = string.Empty;
        /// <summary>
        /// Gets or sets the optional repository package path.<br/>
        /// 저장소 안의 선택적인 package 경로를 가져오거나 설정합니다.
        /// </summary>
        public string packagePath = string.Empty;
        /// <inheritdoc/>
        public override string exactIdentity => CreateUpmInstallation().packageReference;
        /// <inheritdoc/>
        public override IInstallation CreateInstallation(bool isRoot) => CreateUpmInstallation();
        UpmInstallation CreateUpmInstallation() => UpmInstallation.Git(nativeName, repositoryUrl, commit, packagePath, requiredAssemblyReferences);
    }
}
