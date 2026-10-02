#nullable enable
using System;
using System.IO;
using UnityEngine;

namespace RuniOS.PackageManagement.Unity
{
    /// <summary>
    /// Defines a local-directory package without inspecting its content identity.<br/>
    /// 내용 identity를 검사하지 않고 local directory package를 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "LocalPackage", menuName = "Runiverse OS/Installer/Local Package")]
    public sealed class LocalPackage : PackageAsset
    {
        /// <summary>
        /// Gets or sets a package directory, absolute or relative to the project root.<br/>
        /// 절대 경로 또는 프로젝트 root 기준 상대 경로의 package directory를 가져오거나 설정합니다.
        /// </summary>
        public string packagePath = string.Empty;
        /// <inheritdoc/>
        public override string exactIdentity
        {
            get
            {
                if (string.IsNullOrWhiteSpace(packagePath)) throw new ArgumentException("A local package path is required.", nameof(packagePath));
                return "local:" + packagePath.Replace('\\', '/');
            }
        }
        /// <inheritdoc/>
        public override IInstallation CreateInstallation(bool isRoot) => UpmInstallation.Local(nativeName,
            Path.GetFullPath(packagePath, Path.GetDirectoryName(Application.dataPath)!));
    }
}
