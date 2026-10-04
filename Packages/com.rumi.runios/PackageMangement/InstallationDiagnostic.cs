#nullable enable
using System;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Reports an execution failure independently of graph diagnostics.<br/>
    /// graph diagnostic과 독립적으로 실행 실패를 보고합니다.
    /// </summary>
    public sealed class InstallationDiagnostic
    {
        /// <summary>
        /// Gets the implementation-defined diagnostic code.<br/>
        /// 구현이 정의한 diagnostic 코드를 가져옵니다.
        /// </summary>
        public string code { get; }
        /// <summary>
        /// Gets the failure explanation.<br/>
        /// 실패 설명을 가져옵니다.
        /// </summary>
        public string message { get; }
        /// <summary>
        /// Creates an execution diagnostic.<br/>
        /// 실행 diagnostic을 생성합니다.
        /// </summary>
        /// <param name="code">
        /// The diagnostic code.<br/>
        /// diagnostic 코드입니다.
        /// </param>
        /// <param name="message">
        /// The failure explanation.<br/>
        /// 실패 설명입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when an argument is null.<br/>
        /// 인수가 null이면 발생합니다.
        /// </exception>
        public InstallationDiagnostic(string code, string message)
        {
            this.code = code ?? throw new ArgumentNullException(nameof(code));
            this.message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
