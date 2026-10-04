#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.PackageManager;

namespace RuniOS.PackageManagement.Unity
{
    /// <summary>
    /// Satisfies requirements only when the named embedded packages and required assembly-definition assets exist.<br/>
    /// 지정한 embedded package와 필수 어셈블리 정의 에셋이 존재할 때만 요구사항을 만족시킵니다.
    /// </summary>
    public sealed class EmbeddedExecutor : IInstallationExecutor<EmbeddedInstallation>, IInstallationPreviewer<EmbeddedInstallation>
    {
        /// <inheritdoc/>
        public async IAsyncEnumerable<InstallationPreview> PreviewAsync(IEnumerable<EmbeddedInstallation> installations, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (installations is null) throw new ArgumentNullException(nameof(installations));
            HashSet<string> names = await GetEmbeddedNamesAsync(cancellationToken).ConfigureAwait(false);
            foreach (EmbeddedInstallation installation in installations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                InstallationDiagnostic[] diagnostics = await UnityEditorThread.RunAsync(() => AssemblyRequirementUtility.Observe(installation.packageName, installation.requiredAssemblyReferences), cancellationToken).ConfigureAwait(false);
                if (diagnostics.Length != 0)
                {
                    yield return new InstallationPreview(installation, InstallationPreviewStatus.Failed, diagnostics);
                    continue;
                }
                yield return names.Contains(installation.packageName)
                    ? new InstallationPreview(installation, InstallationPreviewStatus.Satisfied)
                    : new InstallationPreview(installation, InstallationPreviewStatus.Failed, MissingDiagnostics(installation));
            }
        }
        /// <inheritdoc/>
        public async IAsyncEnumerable<InstallationResult> EnsureAsync(IEnumerable<EmbeddedInstallation> installations, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (installations is null) throw new ArgumentNullException(nameof(installations));
            HashSet<string> names = await GetEmbeddedNamesAsync(cancellationToken).ConfigureAwait(false);
            foreach (EmbeddedInstallation installation in installations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                InstallationDiagnostic[] diagnostics = await UnityEditorThread.RunAsync(() => AssemblyRequirementUtility.Observe(installation.packageName, installation.requiredAssemblyReferences), cancellationToken).ConfigureAwait(false);
                if (diagnostics.Length != 0) yield return new InstallationResult(installation, false, diagnostics);
                else if (names.Contains(installation.packageName)) yield return new InstallationResult(installation, true);
                else yield return new InstallationResult(installation, false, MissingDiagnostics(installation));
            }
        }
        static async Task<HashSet<string>> GetEmbeddedNamesAsync(CancellationToken cancellationToken)
        {
            PackageInfo[] infos = await UnityEditorThread.RunAsync(() => PackageInfo.GetAllRegisteredPackages(), cancellationToken).ConfigureAwait(false);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (PackageInfo info in infos)
                if (info.source == PackageSource.Embedded) names.Add(info.name);
            return names;
        }
        static InstallationDiagnostic[] MissingDiagnostics(EmbeddedInstallation installation) => new[]
        {
            new InstallationDiagnostic("embedded:missing", $"Required embedded package '{installation.packageName}' is not present in this project.")
        };
    }
}
