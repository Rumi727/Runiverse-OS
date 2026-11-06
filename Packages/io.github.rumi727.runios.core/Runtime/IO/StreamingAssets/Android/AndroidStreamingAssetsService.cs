#nullable enable
using RuniOS.Resource;
using RuniOS.Services;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.IO
{
    /// <summary>
    /// Provides the streaming assets packed in the Android application through <see cref="AndroidStreamingIOProvider"/>.<br/>
    /// Android 애플리케이션에 포함된 StreamingAssets를 <see cref="AndroidStreamingIOProvider"/>로 제공합니다.
    /// </summary>
    public sealed partial class AndroidStreamingAssetsService : IStreamingAssetsService
    {
        AndroidStreamingAssetsService() { }

        /// <summary>
        /// Gets the Android streaming assets service instance.<br/>
        /// Android StreamingAssets 서비스 인스턴스를 가져옵니다.
        /// </summary>
        public static AndroidStreamingAssetsService instance { get; } = new();

        /// <summary>
        /// The identifier of the Android streaming assets service.<br/>
        /// Android StreamingAssets 서비스의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "streaming-assets/android");
        Identifier IService.id => id;

        /// <inheritdoc/>
        public IIOProvider provider { get; } = new AndroidStreamingIOProvider();

        [OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ServiceRegistry.Register(instance);

        [OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ServiceRegistry.Unregister(instance);
    }
}
