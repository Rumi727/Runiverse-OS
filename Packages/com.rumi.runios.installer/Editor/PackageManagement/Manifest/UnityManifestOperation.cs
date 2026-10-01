#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Planning;
using RuniOS.PackageManagement.Unity.Editor.Internal;

namespace RuniOS.PackageManagement.Unity.Editor.Manifest
{
    /// <summary>
    /// Contains a composed manifest transaction, preconditions and post-resolution expectations.<br/>
    /// 합성된 manifest 전이, 전제조건 및 해석 이후 기대 상태를 포함합니다.
    /// </summary>
    public sealed class UnityManifestOperation : InstallOperation
    {
        /// <inheritdoc/>
        public const string operationKind = "unity:manifest-transaction";
        /// <summary>
        /// Gets the composed manifest preview without performing mutation.<br/>
        /// 변경 없이 합성된 manifest preview를 가져옵니다.
        /// </summary>
        public string manifestJson { get; }
        /// <summary>
        /// Gets the ownership state to commit after successful verification.<br/>
        /// 성공적인 검증 이후 기록할 소유권 상태를 가져옵니다.
        /// </summary>
        public string? ownershipJson { get; }
        /// <summary>
        /// Gets immutable observed resource fingerprints.<br/>
        /// 관측된 불변 리소스 지문을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<string, string> preconditions { get; }
        /// <summary>
        /// Gets immutable identities to verify after native resolution.<br/>
        /// 고유 해석 이후 검증할 불변 식별자를 가져옵니다.
        /// </summary>
        public IReadOnlyList<ManifestPackageExpectation> expectations { get; }
        /// <summary>
        /// Gets package identifiers expected to leave effective UPM state.<br/>
        /// 실제 UPM 상태에서 제거될 패키지 식별자를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageId> removals { get; }
        internal UnityManifestOperation(string manifestJson, string? ownershipJson, IReadOnlyDictionary<string, string> preconditions,
            IEnumerable<ManifestPackageExpectation> expectations, IEnumerable<PackageId> removals) : base(operationKind)
        {
            this.manifestJson = manifestJson; this.ownershipJson = ownershipJson;
            this.preconditions = new ReadOnlyDictionary<string, string>(preconditions.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));
            this.expectations = AdapterData.List(expectations); this.removals = AdapterData.List(removals);
        }
    }
}
