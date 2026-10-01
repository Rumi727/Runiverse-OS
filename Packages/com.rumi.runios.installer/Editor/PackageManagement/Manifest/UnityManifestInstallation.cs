#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Unity.Editor.Manifest
{
    /// <summary>
    /// Installs a package through a direct project manifest dependency.<br/>
    /// 프로젝트 manifest의 직접 의존성으로 패키지를 설치합니다.
    /// </summary>
    public sealed class UnityManifestInstallation : PackageInstallation
    {
        /// <inheritdoc/>
        public const string installationKind = "unity:manifest";
        /// <summary>
        /// Gets whether installation is represented by a direct manifest entry.<br/>
        /// 설치가 직접 manifest 항목으로 표현되는지 여부를 가져옵니다.
        /// </summary>
        public bool isDirect { get; }
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="isDirect">
        /// Whether the representation is a direct manifest entry.<br/>
        /// 표현이 직접 manifest 항목인지 여부입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public UnityManifestInstallation(bool isDirect = true) : base(installationKind, isDirect ? "project-manifest" : "resolved-dependency") => this.isDirect = isDirect;
    }
}
