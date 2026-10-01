#nullable enable
using System.Linq;
using RuniOS.PackageManagement.Providers;

namespace RuniOS.PackageManagement.Unity.Editor.Execution
{
    /// <summary>
    /// Reports durable recovery evidence and whether effective UPM state was verified.<br/>
    /// 영속적인 복구 증거와 실제 UPM 상태 검증 여부를 보고합니다.
    /// </summary>
    public sealed class ManifestExecutionReceipt : OperationReceipt
    {
        /// <summary>
        /// Gets the durable transaction journal location.<br/>
        /// 영속적인 전이 journal 위치를 가져옵니다.
        /// </summary>
        public string journalPath { get; }
        /// <summary>
        /// Gets whether the transaction applied its manifest.<br/>
        /// 전이가 manifest를 적용했는지 여부를 가져옵니다.
        /// </summary>
        public bool manifestApplied { get; }
        /// <summary>
        /// Gets whether effective UPM identities were verified.<br/>
        /// 실제 UPM 식별자가 검증되었는지 여부를 가져옵니다.
        /// </summary>
        public bool verified { get; }
        internal ManifestExecutionReceipt(string journalPath, bool manifestApplied, bool verified)
        {
            this.journalPath = journalPath; this.manifestApplied = manifestApplied; this.verified = verified;
        }
    }
}
