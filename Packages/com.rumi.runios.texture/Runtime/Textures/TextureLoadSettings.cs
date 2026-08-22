#nullable enable
namespace RuniOS.Textures
{
    /// <summary>
    /// Specifies runtime texture loading behavior.<br/>
    /// 런타임 텍스처 로드 동작을 지정합니다.
    /// </summary>
    public readonly struct TextureLoadSettings
    {
        /// <summary>
        /// Gets the default texture load settings.<br/>
        /// 기본 텍스처 로드 설정을 가져옵니다.
        /// </summary>
        public static TextureLoadSettings defaultValue => default;

        /// <summary>
        /// Gets the mipmap generation settings.<br/>
        /// 밉맵 생성 설정을 가져옵니다.
        /// </summary>
        public TextureMipmapSettings mipmaps { get; }

        /// <summary>
        /// Gets whether the resulting texture treats pixel values as linear data.<br/>
        /// 결과 텍스처가 픽셀 값을 선형 데이터로 해석하는지 여부를 가져옵니다.
        /// </summary>
        public bool linear { get; }

        /// <summary>
        /// Gets whether the CPU-side texture data is discarded after upload.<br/>
        /// 업로드 후 CPU 측 텍스처 데이터를 제거하는지 여부를 가져옵니다.
        /// </summary>
        public bool makeNoLongerReadable { get; }

        /// <summary>
        /// Initializes texture load settings.<br/>
        /// 텍스처 로드 설정을 초기화합니다.
        /// </summary>
        /// <param name="mipmaps">
        /// The mipmap generation settings.<br/>
        /// 밉맵 생성 설정입니다.
        /// </param>
        /// <param name="linear">
        /// Whether the resulting texture treats pixel values as linear data.<br/>
        /// 결과 텍스처가 픽셀 값을 선형 데이터로 해석할지 여부입니다.
        /// </param>
        /// <param name="makeNoLongerReadable">
        /// Whether to discard CPU-side texture data after upload.<br/>
        /// 업로드 후 CPU 측 텍스처 데이터를 제거할지 여부입니다.
        /// </param>
        public TextureLoadSettings(TextureMipmapSettings mipmaps = default, bool linear = false, bool makeNoLongerReadable = false)
        {
            this.mipmaps = mipmaps;
            this.linear = linear;
            this.makeNoLongerReadable = makeNoLongerReadable;
        }
    }

}