#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Diagnostics
{
    /// <summary>
    /// Describes a package-management issue without prescribing presentation.<br/>
    /// 표시 방식을 지정하지 않고 패키지 관리 문제를 나타냅니다.
    /// </summary>
    public sealed class PackageDiagnostic
    {
        /// <summary>
        /// Gets the namespaced diagnostic code available for frontend translation.<br/>
        /// 프런트엔드 번역에 사용할 수 있는 네임스페이스를 가진 진단 코드를 가져옵니다.
        /// </summary>
        public string code { get; }
        /// <summary>
        /// Gets the severity used to distinguish blocking errors from non-blocking messages.<br/>
        /// 진행을 차단하는 오류와 차단하지 않는 메시지를 구분하는 심각도를 가져옵니다.
        /// </summary>
        public PackageDiagnosticSeverity severity { get; }
        /// <summary>
        /// Gets the diagnostic stage or package lifecycle phase.<br/>
        /// 진단 단계 또는 패키지 생명주기 단계를 가져옵니다.
        /// </summary>
        public PackageDiagnosticPhase phase { get; }
        /// <summary>
        /// Gets fallback diagnostic text without prescribing its presentation.<br/>
        /// 표시 방식을 지정하지 않는 대체 진단 문자열을 가져옵니다.
        /// </summary>
        public string message { get; }
        /// <summary>
        /// Gets related package identifiers, including a closing identifier for cycles.<br/>
        /// 순환 시 닫는 식별자를 포함하는 관련 패키지 식별자를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageId> dependencyPath { get; }
        /// <summary>
        /// Gets the declaration locations associated with this diagnostic.<br/>
        /// 이 진단과 연결된 선언 위치를 가져옵니다.
        /// </summary>
        public IReadOnlyList<DeclarationLocation> locations { get; }

        /// <summary>
        /// Snapshots issue details independently of frontend presentation.<br/>
        /// 프런트엔드 표시와 독립적으로 문제 정보를 스냅샷으로 저장합니다.
        /// </summary>
        /// <param name="code">
        /// The namespaced diagnostic code.<br/>
        /// 네임스페이스를 가진 진단 코드입니다.
        /// </param>
        /// <param name="severity">
        /// The severity determining whether consumption is blocked.<br/>
        /// 결과 사용을 차단할지 결정하는 심각도입니다.
        /// </param>
        /// <param name="phase">
        /// The diagnostic stage or package lifecycle phase.<br/>
        /// 진단 단계 또는 패키지 생명주기 단계입니다.
        /// </param>
        /// <param name="message">
        /// Fallback diagnostic text.<br/>
        /// 대체 진단 문자열입니다.
        /// </param>
        /// <param name="dependencyPath">
        /// Related package identifiers or a closed cycle path.<br/>
        /// 관련 패키지 식별자 또는 닫힌 순환 경로입니다.
        /// </param>
        /// <param name="locations">
        /// Associated declaration locations.<br/>
        /// 연결된 선언 위치입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the diagnostic code is empty or a location is <see langword="null"/>.<br/>
        /// 진단 코드가 비어 있거나 위치가 <see langword="null"/>이면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a numeric or enumeration argument is outside its accepted range.<br/>
        /// 숫자 또는 열거형 인수가 허용 범위를 벗어나면 발생합니다.
        /// </exception>
        public PackageDiagnostic(string code, PackageDiagnosticSeverity severity, PackageDiagnosticPhase phase,
            string message, IEnumerable<PackageId>? dependencyPath = null, IEnumerable<DeclarationLocation>? locations = null)
        {
            this.code = Snapshots.Text(code, nameof(code));
            if (!Enum.IsDefined(typeof(PackageDiagnosticSeverity), severity))
                throw new ArgumentOutOfRangeException(nameof(severity));
            if (!Enum.IsDefined(typeof(PackageDiagnosticPhase), phase))
                throw new ArgumentOutOfRangeException(nameof(phase));
            this.severity = severity;
            this.phase = phase;
            this.message = message ?? throw new ArgumentNullException(nameof(message));
            this.dependencyPath = Snapshots.List(dependencyPath ?? Array.Empty<PackageId>());
            this.locations = Snapshots.List(locations ?? Array.Empty<DeclarationLocation>());
        }

        internal static PackageDiagnostic Error(string code, PackageDiagnosticPhase phase, string message,
            IEnumerable<PackageId>? path = null, IEnumerable<DeclarationLocation>? locations = null) =>
            new(code, PackageDiagnosticSeverity.Error, phase, message, path, locations);
    }
}
