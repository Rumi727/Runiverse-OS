#nullable enable
using System;
using System.Collections.Generic;
using UnityEditorInternal;

namespace RuniOS.PackageManagement.Unity
{
    static class AssemblyRequirementUtility
    {
        internal static InstallationDiagnostic[] Observe(string packageName, IReadOnlyList<AssemblyDefinitionAsset?> definitions)
        {
            foreach (AssemblyDefinitionAsset? definition in definitions)
                if (definition == null)
                    return new[]
                    {
                        new InstallationDiagnostic("unity:required-assembly-missing",
                            $"A required assembly-definition asset for package '{packageName}' is missing. Import the required integration before installation.")
                    };
            return Array.Empty<InstallationDiagnostic>();
        }
    }
}
