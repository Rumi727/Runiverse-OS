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
        /// Gets the requested mipmap level count.<br/>
        /// 요청한 밉맵 레벨 수를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// A value less than or equal to <c>0</c> generates the full mipmap chain, <c>1</c> disables mipmaps, and a value of <c>2</c> or greater requests that many levels including the base level.<br/>
        /// <c>0</c> 이하는 전체 밉맵 체인을 생성하고, <c>1</c>은 밉맵을 생성하지 않으며, <c>2</c> 이상은 기본 레벨을 포함한 해당 개수의 레벨을 요청합니다.
        /// </remarks>
        public int mipmapCount { get; }

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
        /// <param name="mipmapCount">
        /// The requested mipmap level count. Values less than or equal to <c>0</c> generate the full chain, <c>1</c> disables mipmaps, and values of <c>2</c> or greater include the base level in the requested count.<br/>
        /// 요청한 밉맵 레벨 수입니다. <c>0</c> 이하는 전체 체인을 생성하고, <c>1</c>은 밉맵을 생성하지 않으며, <c>2</c> 이상은 기본 레벨을 포함한 요청 개수로 처리합니다.
        /// </param>
        /// <param name="linear">
        /// Whether the resulting texture treats pixel values as linear data.<br/>
        /// 결과 텍스처가 픽셀 값을 선형 데이터로 해석할지 여부입니다.
        /// </param>
        /// <param name="makeNoLongerReadable">
        /// Whether to discard CPU-side texture data after upload.<br/>
        /// 업로드 후 CPU 측 텍스처 데이터를 제거할지 여부입니다.
        /// </param>
        public TextureLoadSettings(int mipmapCount = 0, bool linear = false, bool makeNoLongerReadable = false)
        {
            this.mipmapCount = mipmapCount;
            this.linear = linear;
            this.makeNoLongerReadable = makeNoLongerReadable;
        }
    }
}