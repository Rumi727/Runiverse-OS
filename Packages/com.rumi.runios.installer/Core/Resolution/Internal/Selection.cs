#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Resolution.Internal
{
    internal sealed class Selection
    {
        internal PackageCandidate candidate { get; }
        internal PackageMetadata metadata { get; }
        internal IReadOnlyList<PackageDiagnostic> diagnostics { get; }

        internal Selection(PackageCandidate candidate, PackageMetadata metadata, IEnumerable<PackageDiagnostic> diagnostics)
        {
            this.candidate = candidate;
            this.metadata = metadata;
            this.diagnostics = Snapshots.List(diagnostics);
        }
    }
}
