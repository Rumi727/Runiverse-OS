#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.NBS;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Resource.NBS
{
    /// <summary>
    /// Registers the files below <c>assets/&lt;namespace&gt;/nbses</c> as <see cref="NoteBlockClip"/> resources.<br/>
    /// <c>assets/&lt;namespace&gt;/nbses</c> 아래 파일을 <see cref="NoteBlockClip"/> 리소스로 등록합니다.
    /// </summary>
    public sealed partial class NBSAssetRegistry : SimpleAssetRegistry<NBSAssetHandle>
    {
        /// <inheritdoc cref="registryId"/>
        // Temporary identifier while the previous NBS package still registers runios:nbs.
        public static readonly Identifier id = new Identifier("runios", "nbs_next");

        /// <inheritdoc/>
        public override Identifier registryId => id;

        /// <inheritdoc/>
        public override RuniPath registryName => RuniPath.From("nbses");

        /// <inheritdoc/>
        public override Type providedAssetType => typeof(NoteBlockClip);

        /// <inheritdoc/>
        public override IPatternMatcher assetMatcher => IPatternMatcher.nbsMatcher;

        [OnCodeLoaded]
        static void OnCodeLoaded() => AssetRegistryManager.Register<NBSAssetRegistry>();

        [OnCodeUnloading]
        static void OnCodeUnloading() => AssetRegistryManager.Unregister<NBSAssetRegistry>();

        /// <inheritdoc/>
        protected override UniTask<NBSAssetHandle> CreateHandle(AssetDiscoveryContext context, Identifier identifier, IONode node, FileMetaData fileMetaData) =>
            UniTask.FromResult(new NBSAssetHandle(node, fileMetaData));
    }
}
