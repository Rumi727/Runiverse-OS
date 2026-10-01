#nullable enable
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Manifest;
using RuniOS.PackageManagement.Unity.Editor.Registry;

namespace RuniOS.PackageManagement.Unity.Editor.Builtin
{
    internal sealed class UnityBuiltinArtifact : PackageArtifactReference, IUnityManifestArtifact
    {
        internal const string artifactKind = "unity:builtin-package";
        public string dependencyValue { get; }
        public string packageIdentifier { get; }
        readonly string editorVersion;
        public PackageIdentity GetInstalledIdentity(PackageId id) => new(id, UnityBuiltinSource.sourceKind, id.value, editorVersion + ":" + dependencyValue, dependencyValue);
        public ScopedRegistryDefinition? registry => null;
        internal UnityBuiltinArtifact(PackageId id, string version, string editorVersion) : base(artifactKind)
        {
            this.editorVersion = editorVersion;
            dependencyValue = version; packageIdentifier = id.value + "@" + version;
        }
    }
}
