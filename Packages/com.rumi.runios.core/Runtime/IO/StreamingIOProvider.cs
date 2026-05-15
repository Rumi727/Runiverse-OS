#nullable enable
namespace RuniOS.IO
{
    /// <summary>
    /// Provides the platform-specific provider used for application streaming assets.<br/>
    /// 애플리케이션 StreamingAssets에 사용하는 플랫폼별 프로바이더를 제공합니다.
    /// </summary>
    public static class StreamingIOProvider
    {
        /// <summary>
        /// Gets the streaming-assets provider for the current platform.<br/>
        /// 현재 플랫폼의 StreamingAssets 프로바이더를 가져옵니다.
        /// </summary>
        public static IIOProvider instance { get; } =
#if UNITY_ANDROID
            new AndroidStreamingIOProvider();
#else
            new PhysicalIOProvider((PhysicalPath)Application.streamingAssetsPath);
#endif
    }
}
