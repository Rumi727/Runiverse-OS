#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Reports the final outcome for one descriptor and execution source.<br/>
    /// 하나의 descriptor와 실행 주체에 대한 최종 결과를 보고합니다.
    /// </summary>
    public sealed class InstallationResult
    {
        /// <summary>
        /// Gets the original input descriptor.<br/>
        /// 원래 입력 descriptor를 가져옵니다.
        /// </summary>
        public IInstallation installation { get; }
        /// <summary>
        /// Gets the executor attached by the runner; unsupported descriptors have no executor.<br/>
        /// Runner가 지정한 executor를 가져오며 지원되지 않는 descriptor에는 executor가 없습니다.
        /// </summary>
        public IInstallationExecutor? executor { get; }
        /// <summary>
        /// Gets whether this execution satisfied the requirements.<br/>
        /// 이 실행이 요구사항을 만족시켰는지 여부를 가져옵니다.
        /// </summary>
        public bool succeeded { get; }
        /// <summary>
        /// Gets execution diagnostics.<br/>
        /// 실행 diagnostic을 가져옵니다.
        /// </summary>
        public IReadOnlyList<InstallationDiagnostic> diagnostics { get; }
        /// <summary>
        /// Creates a final installation outcome.<br/>
        /// 최종 설치 결과를 생성합니다.
        /// </summary>
        /// <param name="installation">
        /// The original input descriptor.<br/>
        /// 원래 입력 descriptor입니다.
        /// </param>
        /// <param name="succeeded">
        /// Whether the requirements were satisfied.<br/>
        /// 요구사항의 충족 여부입니다.
        /// </param>
        /// <param name="diagnostics">
        /// Optional failure details.<br/>
        /// 선택적인 실패 상세 정보입니다.
        /// </param>
        /// <param name="executor">
        /// Optional execution source, attached by the runner.<br/>
        /// Runner가 지정하는 선택적인 실행 주체입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when installation is null.<br/>
        /// installation이 null이면 발생합니다.
        /// </exception>
        public InstallationResult(IInstallation installation, bool succeeded, IEnumerable<InstallationDiagnostic>? diagnostics = null, IInstallationExecutor? executor = null)
        {
            this.installation = installation ?? throw new ArgumentNullException(nameof(installation));
            this.succeeded = succeeded;
            this.executor = executor;
            this.diagnostics = Array.AsReadOnly(diagnostics?.ToArray() ?? Array.Empty<InstallationDiagnostic>());
        }
        internal InstallationResult(InstallationResult result, IInstallationExecutor executor)
        {
            installation = result.installation;
            succeeded = result.succeeded;
            diagnostics = result.diagnostics;
            this.executor = executor;
        }
    }
}
