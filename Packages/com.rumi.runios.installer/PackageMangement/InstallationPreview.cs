#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Reports an advisory observation for one descriptor and executor.<br/>
    /// 하나의 descriptor와 executor에 대한 advisory 관측을 보고합니다.
    /// </summary>
    public sealed class InstallationPreview
    {
        /// <summary>
        /// Gets the original input descriptor.<br/>
        /// 원래 입력 descriptor를 가져옵니다.
        /// </summary>
        public IInstallation installation { get; }
        /// <summary>
        /// Gets the executor attached by the runner, or <see langword="null"/> for unsupported inputs.<br/>
        /// Runner가 지정한 executor를 가져오며 지원되지 않는 입력에는 <see langword="null"/>입니다.
        /// </summary>
        public IInstallationExecutor? executor { get; }
        /// <summary>
        /// Gets the current advisory status.<br/>
        /// 현재 advisory 상태를 가져옵니다.
        /// </summary>
        public InstallationPreviewStatus status { get; }
        /// <summary>
        /// Gets observation diagnostics.<br/>
        /// 관측 diagnostic을 가져옵니다.
        /// </summary>
        public IReadOnlyList<InstallationDiagnostic> diagnostics { get; }
        /// <summary>
        /// Captures an observation without retaining native environment state.<br/>
        /// native 환경 상태를 보관하지 않고 관측 결과를 담습니다.
        /// </summary>
        /// <param name="installation">
        /// The original input descriptor.<br/>
        /// 원래 입력 descriptor입니다.
        /// </param>
        /// <param name="status">
        /// The observed status.<br/>
        /// 관측한 상태입니다.
        /// </param>
        /// <param name="diagnostics">
        /// Optional observation failure details.<br/>
        /// 선택적인 관측 실패 상세 정보입니다.
        /// </param>
        /// <param name="executor">
        /// The optional executor attached by the runner.<br/>
        /// Runner가 지정하는 선택적인 executor입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="installation"/> is <see langword="null"/>.<br/>
        /// <paramref name="installation"/>이 <see langword="null"/>이면 발생합니다.
        /// </exception>
        public InstallationPreview(IInstallation installation, InstallationPreviewStatus status, IEnumerable<InstallationDiagnostic>? diagnostics = null, IInstallationExecutor? executor = null)
        {
            this.installation = installation ?? throw new ArgumentNullException(nameof(installation));
            this.status = status;
            this.executor = executor;
            this.diagnostics = Array.AsReadOnly(diagnostics?.ToArray() ?? Array.Empty<InstallationDiagnostic>());
        }
        internal InstallationPreview(InstallationPreview preview, IInstallationExecutor executor)
        {
            installation = preview.installation;
            status = preview.status;
            diagnostics = preview.diagnostics;
            this.executor = executor;
        }
    }
}
