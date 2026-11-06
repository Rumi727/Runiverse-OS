#nullable enable
using RuniOS.Services;

namespace RuniOS.IO
{
    /// <summary>
    /// Provides the streaming assets of the application as an <see cref="IIOProvider"/>.<br/>
    /// 애플리케이션의 StreamingAssets를 <see cref="IIOProvider"/>로 제공합니다.
    /// </summary>
    /// <remarks>
    /// Each platform provides its own implementation, and the implementations are separated by the platform restrictions of their assemblies.<br/>
    /// 플랫폼마다 자체 구현을 제공하며, 구현은 어셈블리의 플랫폼 제한으로 구분됩니다.
    /// </remarks>
    public interface IStreamingAssetsService : IService
    {
        /// <summary>
        /// The name of the folder that contains streaming assets.<br/>
        /// StreamingAssets를 담는 폴더의 이름입니다.
        /// </summary>
        public const string streamingAssetsFolderName = "StreamingAssets";

        /// <summary>
        /// Gets the provider whose root is the streaming assets.<br/>
        /// StreamingAssets를 루트로 하는 프로바이더를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// The service owns the provider.<br/>
        /// 프로바이더는 서비스가 소유합니다.
        /// </remarks>
        IIOProvider provider { get; }
    }
}
