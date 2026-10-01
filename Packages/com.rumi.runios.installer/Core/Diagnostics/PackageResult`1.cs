#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Diagnostics
{
    /// <summary>
    /// Returns a value with diagnostics; errors prevent successful consumption.<br/>
    /// 값과 진단을 반환하며 오류가 있으면 성공 결과로 사용할 수 없습니다.
    /// </summary>
    /// <typeparam name="T">
    /// The result value type.<br/>
    /// 결과 값의 타입입니다.
    /// </typeparam>
    public sealed class PackageResult<T> where T : class
    {
        /// <summary>
        /// Gets the optional result; consumers must also check <see cref="succeeded"/>.<br/>
        /// 선택적 결과를 가져오며 사용 시 <see cref="succeeded"/>도 확인해야 합니다.
        /// </summary>
        public T? value { get; }
        /// <summary>
        /// Gets the immutable diagnostics accompanying this result.<br/>
        /// 이 결과와 함께 제공되는 불변 진단을 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageDiagnostic> diagnostics { get; }
        /// <summary>
        /// Gets whether a result value exists without error diagnostics.<br/>
        /// 오류 진단 없이 결과 값이 존재하는지 여부를 가져옵니다.
        /// </summary>
        public bool succeeded => value is not null && !diagnostics.Any(x => x.severity == PackageDiagnosticSeverity.Error);

        /// <summary>
        /// Associates an optional value with an immutable diagnostic snapshot.<br/>
        /// 선택적 값과 불변 진단 스냅샷을 연결합니다.
        /// </summary>
        /// <param name="value">
        /// The immutable value or identifier to retain.<br/>
        /// 보존할 불변 값 또는 식별자입니다.
        /// </param>
        /// <param name="diagnostics">
        /// Diagnostics accompanying the result or decision.<br/>
        /// 결과 또는 결정과 함께 제공되는 진단입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="diagnostics"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="diagnostics"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public PackageResult(T? value, IEnumerable<PackageDiagnostic>? diagnostics = null)
        {
            this.value = value;
            this.diagnostics = Snapshots.List(diagnostics ?? Array.Empty<PackageDiagnostic>());
        }
    }
}
