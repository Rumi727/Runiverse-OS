#nullable enable
namespace RuniOS.Reflection
{
    /// <summary>
    /// Represents the nullability annotation state of a type usage.<br/>
    /// 타입 사용 지점의 nullable 주석 상태를 나타냅니다.
    /// </summary>
    public enum NullState
    {
        /// <summary>
        /// The nullability is not known, for example because the member has no annotations.<br/>
        /// 주석이 없는 경우처럼 nullable 여부를 알 수 없습니다.
        /// </summary>
        Unknown,

        /// <summary>
        /// The value is annotated as not null.<br/>
        /// 값이 null이 아니도록 주석되어 있습니다.
        /// </summary>
        NotNull,

        /// <summary>
        /// The value is annotated as nullable.<br/>
        /// 값이 null일 수 있도록 주석되어 있습니다.
        /// </summary>
        Nullable
    }
}
