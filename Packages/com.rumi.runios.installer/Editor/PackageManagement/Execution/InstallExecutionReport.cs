#nullable enable
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Providers;
using RuniOS.PackageManagement.Unity.Editor.Internal;

namespace RuniOS.PackageManagement.Unity.Editor.Execution
{
    /// <summary>
    /// Retains receipts from completed or partially applied operations.<br/>
    /// 완료되거나 부분적으로 적용된 작업의 receipt를 보존합니다.
    /// </summary>
    public sealed class InstallExecutionReport
    {
        /// <summary>
        /// Gets immutable receipts including partial execution evidence.<br/>
        /// 부분 실행 증거를 포함한 불변 receipt를 가져옵니다.
        /// </summary>
        public IReadOnlyList<OperationReceipt> receipts { get; }
        internal InstallExecutionReport(IEnumerable<OperationReceipt> receipts) => this.receipts = AdapterData.List(receipts);
    }
}
