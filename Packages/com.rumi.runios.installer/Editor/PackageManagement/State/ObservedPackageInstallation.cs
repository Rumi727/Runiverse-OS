#nullable enable
using System.Linq;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Unity.Editor.State
{
    internal sealed class ObservedPackageInstallation : PackageInstallation
    {
        internal ObservedPackageInstallation(string key) : base("unity:external-package", key) { }
    }
}
