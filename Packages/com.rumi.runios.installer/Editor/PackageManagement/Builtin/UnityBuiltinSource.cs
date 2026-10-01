#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Internal;

namespace RuniOS.PackageManagement.Unity.Editor.Builtin
{
    /// <summary>
    /// Identifies a package supplied by the current Unity Editor installation.<br/>
    /// 현재 Unity Editor 설치가 제공하는 패키지를 식별합니다.
    /// </summary>
    public sealed class UnityBuiltinSource : PackageSource
    {
        /// <inheritdoc/>
        public const string sourceKind = "unity:builtin";
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="id">
        /// The logical package identifier.<br/>
        /// 논리적 패키지 식별자입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public UnityBuiltinSource(PackageId id) : base(sourceKind, AdapterData.PackageName(id.value)) { }
    }
}
