#nullable enable
using RuniOS.IO;
using RuniOS.Resource;
using RuniOS.Services;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Editor
{
    /// <summary>
    /// Provides the project streaming assets and the streaming assets of every registered package in the Editor.<br/>
    /// 에디터에서 프로젝트의 StreamingAssets와 등록된 모든 패키지의 StreamingAssets를 제공합니다.
    /// </summary>
    /// <remarks>
    /// The project streaming assets take priority over the package streaming assets, so the end user always has the final say over the resources.<br/>
    /// 프로젝트 StreamingAssets가 패키지 StreamingAssets보다 우선하므로, 리소스에 대한 최종 결정권은 항상 최종 사용자에게 있습니다.
    /// </remarks>
    public sealed class EditorStreamingAssetsService : IStreamingAssetsService
    {
        EditorStreamingAssetsService() { }

        /// <summary>
        /// Gets the Editor streaming assets service instance.<br/>
        /// 에디터 StreamingAssets 서비스 인스턴스를 가져옵니다.
        /// </summary>
        public static EditorStreamingAssetsService instance { get; } = new();

        /// <summary>
        /// The identifier of the Editor streaming assets service.<br/>
        /// 에디터 StreamingAssets 서비스의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "streaming-assets/editor");
        Identifier IService.id => id;

        IIOProvider? _provider;

        /// <inheritdoc/>
        public IIOProvider provider => _provider ??= CreateProvider();

        static GroupIOProvider CreateProvider()
        {
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .Select(x => new PhysicalIOProvider((PhysicalPath)x.resolvedPath / IStreamingAssetsService.streamingAssetsFolderName, SandboxPolicy.Unrestricted))
                .OfType<IIOProvider>();

            return new GroupIOProvider([new PhysicalIOProvider((PhysicalPath)Application.streamingAssetsPath, SandboxPolicy.Unrestricted), .. packages]);
        }

        [OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ServiceRegistry.Register(instance);

        [OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ServiceRegistry.Unregister(instance);
    }
}
