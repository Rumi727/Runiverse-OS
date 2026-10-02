#nullable enable
using UnityEngine;

namespace RuniOS.PackageManagement.Unity
{
    /// <summary>
    /// Defines a dependency on an embedded package that must already exist.<br/>
    /// 이미 존재해야 하는 embedded package에 대한 dependency를 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "EmbeddedPackage", menuName = "Runiverse OS/Installer/Embedded Package")]
    public sealed class EmbeddedPackage : PackageAsset
    {
        /// <inheritdoc/>
        public override string exactIdentity => "embedded:" + nativeName;
        /// <inheritdoc/>
        public override IInstallation CreateInstallation() => new EmbeddedInstallation(nativeName);
    }
}
