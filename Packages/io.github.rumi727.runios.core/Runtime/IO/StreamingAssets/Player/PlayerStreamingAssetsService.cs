#nullable enable
using RuniOS.Resource;
using RuniOS.Services;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace RuniOS.IO
{
    /// <summary>
    /// Provides the streaming assets of player platforms that expose them as a physical directory.<br/>
    /// StreamingAssets를 물리 디렉터리로 노출하는 플레이어 플랫폼에서 StreamingAssets를 제공합니다.
    /// </summary>
    public sealed partial class PlayerStreamingAssetsService : IStreamingAssetsService
    {
        PlayerStreamingAssetsService() { }

        /// <summary>
        /// Gets the player streaming assets service instance.<br/>
        /// 플레이어 StreamingAssets 서비스 인스턴스를 가져옵니다.
        /// </summary>
        public static PlayerStreamingAssetsService instance { get; } = new();

        /// <summary>
        /// The identifier of the player streaming assets service.<br/>
        /// 플레이어 StreamingAssets 서비스의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "streaming-assets/player");
        Identifier IService.id => id;

        IIOProvider? _provider;

        /// <inheritdoc/>
        public IIOProvider provider => _provider ??= new PhysicalIOProvider((PhysicalPath)Application.streamingAssetsPath, SandboxPolicy.Unrestricted);

        [OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ServiceRegistry.Register(instance);

        [OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ServiceRegistry.Unregister(instance);
    }
}
