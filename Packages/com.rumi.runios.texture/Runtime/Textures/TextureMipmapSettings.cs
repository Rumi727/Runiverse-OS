#nullable enable
namespace RuniOS.Textures
{
    /// <summary>
    /// Specifies mipmap generation behavior.<br/>
    /// 밉맵 생성 동작을 지정합니다.
    /// </summary>
    public readonly struct TextureMipmapSettings
    {
        /// <summary>
        /// Gets the mipmap count selection mode.<br/>
        /// 밉맵 개수 선택 모드를 가져옵니다.
        /// </summary>
        public TextureMipmapMode mode { get; }

        /// <summary>
        /// Gets the requested level count including the base level.<br/>
        /// 기본 레벨을 포함해 요청한 레벨 수를 가져옵니다.
        /// </summary>
        public int count { get; }

        /// <summary>
        /// Gets settings that generate every level down to 1x1.<br/>
        /// 1x1까지 모든 레벨을 생성하는 설정을 가져옵니다.
        /// </summary>
        public static TextureMipmapSettings full => default;

        /// <summary>
        /// Gets settings that generate only the base level.<br/>
        /// 기본 레벨만 생성하는 설정을 가져옵니다.
        /// </summary>
        public static TextureMipmapSettings none => new TextureMipmapSettings(TextureMipmapMode.none, 1);

        TextureMipmapSettings(TextureMipmapMode mode, int count)
        {
            this.mode = mode;
            this.count = count;
        }

        /// <summary>
        /// Creates settings for an explicit mipmap level count.<br/>
        /// 명시적인 밉맵 레벨 수를 사용하는 설정을 생성합니다.
        /// </summary>
        /// <param name="count">
        /// The level count including the base level.<br/>
        /// 기본 레벨을 포함한 레벨 수입니다.
        /// </param>
        /// <returns>
        /// Settings that request exactly <paramref name="count"/> levels.<br/>
        /// 정확히 <paramref name="count"/>개 레벨을 요청하는 설정을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="count"/> is less than one.<br/>
        /// <paramref name="count"/>가 1보다 작은 경우 발생합니다.
        /// </exception>
        public static TextureMipmapSettings Explicit(int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Mipmap count must be at least one.");
            return new TextureMipmapSettings(TextureMipmapMode.explicitCount, count);
        }
    }
}