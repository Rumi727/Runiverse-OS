#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Unity.Editor.Metadata
{
    /// <summary>
    /// Supplies immutable parent identity and registry routing to declaration readers.<br/>
    /// 선언 리더에 불변 부모 식별자와 레지스트리 연결 정보를 제공합니다.
    /// </summary>
    public sealed class UpmDeclarationContext
    {
        /// <summary>
        /// Gets the immutable parent candidate of this declaration.<br/>
        /// 이 선언의 불변 부모 후보를 가져옵니다.
        /// </summary>
        public PackageCandidate parent { get; }
        /// <summary>
        /// Gets the registry routing configuration for this declaration.<br/>
        /// 이 선언의 레지스트리 연결 설정을 가져옵니다.
        /// </summary>
        public UpmSourceConfiguration sources { get; }
        /// <summary>
        /// Gets the declaration document and dependency position.<br/>
        /// 선언 문서와 의존성 위치를 가져옵니다.
        /// </summary>
        public DeclarationLocation location { get; }
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="parent">
        /// The immutable candidate owning this dependency declaration.<br/>
        /// 이 의존성 선언을 소유하는 불변 후보입니다.
        /// </param>
        /// <param name="sources">
        /// The immutable registry routing configuration.<br/>
        /// 불변 레지스트리 연결 설정입니다.
        /// </param>
        /// <param name="location">
        /// Optional declaration provenance.<br/>
        /// 선택적인 선언 위치 정보입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public UpmDeclarationContext(PackageCandidate parent, UpmSourceConfiguration sources, DeclarationLocation location)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
            this.sources = sources ?? throw new ArgumentNullException(nameof(sources));
            this.location = location ?? throw new ArgumentNullException(nameof(location));
        }
    }
}
