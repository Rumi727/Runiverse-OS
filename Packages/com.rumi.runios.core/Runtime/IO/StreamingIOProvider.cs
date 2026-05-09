#nullable enable
namespace RuniOS.IO
{
    public static class StreamingIOProvider
    {
        public static IIOProvider instance { get; } =
#if UNITY_ANDROID
            new AndroidStreamingIOProvider();
#else
            new PhysicalIOProvider(Application.streamingAssetsPath);
#endif
    }
}