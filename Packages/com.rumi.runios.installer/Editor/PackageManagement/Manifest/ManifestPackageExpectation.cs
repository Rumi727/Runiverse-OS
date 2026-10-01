#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Unity.Editor.Manifest
{
    /// <summary>
    /// Retains the exact effective identity expected after a manifest transaction.<br/>
    /// manifest 전이 이후 기대하는 정확한 실제 식별자를 보존합니다.
    /// </summary>
    public sealed class ManifestPackageExpectation
    {
        /// <summary>
        /// Gets the selected logical content identity.<br/>
        /// 선택된 논리적 콘텐츠 식별자를 가져옵니다.
        /// </summary>
        public PackageIdentity identity { get; }
        /// <summary>
        /// Gets the native UPM identity expected from the chosen representation.<br/>
        /// 선택한 표현에서 기대하는 UPM 고유 식별자를 가져옵니다.
        /// </summary>
        public PackageIdentity installedIdentity { get; }
        /// <summary>
        /// Gets the UPM request identifier, or <see langword="null"/> for a retained package.<br/>
        /// UPM 요청 식별자를 가져오며 유지할 패키지이면 <see langword="null"/>을 반환합니다.
        /// </summary>
        public string? packageIdentifier { get; }
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="identity">
        /// The selected logical identity.<br/>
        /// 선택된 논리적 식별자입니다.
        /// </param>
        /// <param name="packageIdentifier">
        /// Optional native UPM request identifier.<br/>
        /// 선택적인 UPM 고유 요청 식별자입니다.
        /// </param>
        /// <param name="installedIdentity">
        /// Optional native UPM identity; omission uses the logical identity.<br/>
        /// 선택적인 UPM 고유 식별자이며 생략하면 논리적 식별자를 사용합니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public ManifestPackageExpectation(PackageIdentity identity, string? packageIdentifier = null, PackageIdentity? installedIdentity = null)
        {
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity)); this.packageIdentifier = packageIdentifier;
            this.installedIdentity = installedIdentity ?? identity;
            if (this.installedIdentity.id != identity.id) throw new ArgumentException("Logical and installed package IDs must match.", nameof(installedIdentity));
        }
    }
}
