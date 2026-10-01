#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Internal;

namespace RuniOS.PackageManagement.Unity.Editor.Metadata
{
    /// <summary>
    /// Requires an exact UPM package version; ranges are not inferred.<br/>
    /// 정확한 UPM 패키지 버전을 요구하며 범위를 추측하지 않습니다.
    /// </summary>
    public sealed class UpmVersionConstraint : PackageConstraint
    {
        /// <inheritdoc/>
        public const string constraintKind = "upm:exact-version";
        /// <summary>
        /// Gets the exact declared UPM version.<br/>
        /// 정확하게 선언된 UPM 버전을 가져옵니다.
        /// </summary>
        public string version { get; }
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="version">
        /// The exact UPM version, excluding version ranges.<br/>
        /// 버전 범위를 제외한 정확한 UPM 버전입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public UpmVersionConstraint(string version) : base(constraintKind)
        {
            AdapterData.Text(version, nameof(version));
            if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$"))
                throw new ArgumentException("Only exact UPM versions are supported in this implementation.", nameof(version));
            this.version = version;
        }
    }
}
