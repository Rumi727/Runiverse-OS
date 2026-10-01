#nullable enable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Unity.Editor.State
{
    internal sealed class UnityProjectSnapshotFact : EnvironmentFact
    {
        internal string manifestJson { get; }
        internal string? ownershipJson { get; }
        internal IReadOnlyDictionary<PackageId, PackageIdentity> installedIdentities { get; }
        internal UnityProjectSnapshotFact(string manifestJson, string? ownershipJson, IDictionary<PackageId, PackageIdentity> installedIdentities)
        {
            this.manifestJson = manifestJson; this.ownershipJson = ownershipJson;
            this.installedIdentities = new ReadOnlyDictionary<PackageId, PackageIdentity>(new Dictionary<PackageId, PackageIdentity>(installedIdentities));
        }
    }
}
