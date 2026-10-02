#nullable enable
using System.Collections.Generic;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Reports a definition or dependency graph error with its reference path.<br/>
    /// 참조 경로와 함께 정의 또는 dependency graph 오류를 보고합니다.
    /// </summary>
    public sealed class PackageGraphDiagnostic
    {
        /// <summary>
        /// Gets the graph diagnostic code.<br/>
        /// graph diagnostic 코드를 가져옵니다.
        /// </summary>
        public string code { get; }
        /// <summary>
        /// Gets the error explanation.<br/>
        /// 오류 설명을 가져옵니다.
        /// </summary>
        public string message { get; }
        /// <summary>
        /// Gets the affected definition, if present.<br/>
        /// 존재하는 경우 오류 대상 정의를 가져옵니다.
        /// </summary>
        public IPackage? package { get; }
        /// <summary>
        /// Gets the conflicting or duplicate definition, if present.<br/>
        /// 존재하는 경우 충돌하거나 중복된 정의를 가져옵니다.
        /// </summary>
        public IPackage? relatedPackage { get; }
        /// <summary>
        /// Gets the owner of the failing dependency reference, if present.<br/>
        /// 존재하는 경우 실패한 dependency 참조의 소유자를 가져옵니다.
        /// </summary>
        public IPackage? owner { get; }
        /// <summary>
        /// Gets the failing dependency or root slot, or -1 when not applicable.<br/>
        /// 실패한 dependency 또는 root 슬롯을 가져오며 해당하지 않으면 -1입니다.
        /// </summary>
        public int referenceIndex { get; }
        /// <summary>
        /// Gets the reference path from a selected root to the error.<br/>
        /// 선택한 root에서 오류까지의 참조 경로를 가져옵니다.
        /// </summary>
        public IReadOnlyList<IPackage> path { get; }
        /// <summary>
        /// Gets the path to the previously selected conflicting definition, if applicable.<br/>
        /// 해당하는 경우 먼저 선택된 충돌 정의까지의 경로를 가져옵니다.
        /// </summary>
        public IReadOnlyList<IPackage> relatedPath { get; }
        internal PackageGraphDiagnostic(string code, string message, IPackage? package = null, IPackage? relatedPackage = null, IPackage? owner = null, int referenceIndex = -1, IPackage[]? path = null, IPackage[]? relatedPath = null)
        {
            this.code = code;
            this.message = message;
            this.package = package;
            this.relatedPackage = relatedPackage;
            this.owner = owner;
            this.referenceIndex = referenceIndex;
            this.path = System.Array.AsReadOnly(path ?? System.Array.Empty<IPackage>());
            this.relatedPath = System.Array.AsReadOnly(relatedPath ?? System.Array.Empty<IPackage>());
        }
    }
}
