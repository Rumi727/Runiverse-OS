#nullable enable
using UnityEngine;

namespace RuniOS.PackageManagement.Unity
{
    /// <summary>
    /// Defines an exact registry version with authored direct dependencies.<br/>
    /// 작성한 직접 dependencies와 exact registry version으로 package를 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "RegistryPackage", menuName = "Runiverse OS/Installer/Registry Package")]
    public sealed class RegistryPackage : PackageAsset
    {
        /// <summary>
        /// Gets or sets the exact version string.<br/>
        /// exact version 문자열을 가져오거나 설정합니다.
        /// </summary>
        public string version = string.Empty;
        /// <summary>
        /// Gets or sets the optional scoped-registry display name.<br/>
        /// 선택적인 scoped registry 표시 이름을 가져오거나 설정합니다.
        /// </summary>
        public string registryName = string.Empty;
        /// <summary>
        /// Gets or sets the scoped-registry URL; empty text uses the configured default registry.<br/>
        /// scoped registry URL을 가져오거나 설정하며 빈 문자열은 설정된 기본 registry를 사용합니다.
        /// </summary>
        public string registryUrl = string.Empty;
        /// <summary>
        /// Gets or sets the scopes required when a registry URL is provided.<br/>
        /// registry URL을 제공할 때 필요한 scope들을 가져오거나 설정합니다.
        /// </summary>
        public string[] registryScopes = System.Array.Empty<string>();
        /// <inheritdoc/>
        public override string exactIdentity
        {
            get
            {
                UpmInstallation requirement = CreateUpmInstallation();
                return (requirement.registry?.url ?? "default-registry") + "|" + requirement.packageReference;
            }
        }
        /// <inheritdoc/>
        public override IInstallation CreateInstallation(bool isRoot) => CreateUpmInstallation(isRoot);
        UpmInstallation CreateUpmInstallation(bool ensurePackage = true)
        {
            ScopedRegistryDefinition? registry = string.IsNullOrEmpty(registryUrl) ? null : new ScopedRegistryDefinition(registryName, registryUrl, registryScopes);
            return UpmInstallation.Registry(nativeName, version, registry, ensurePackage, requiredAssemblyReferences);
        }
    }
}
