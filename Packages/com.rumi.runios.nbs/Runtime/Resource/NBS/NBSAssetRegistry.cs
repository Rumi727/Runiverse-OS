#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.NBS;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Resource.NBS
{
    /// <summary>
    /// Registers files below <c>assets/&lt;namespace&gt;/nbs</c> as scoped <see cref="NoteBlockClip"/> resources.<br/>
    /// <c>assets/&lt;namespace&gt;/nbs</c> 아래 파일을 스코프 기반 <see cref="NoteBlockClip"/> 리소스로 등록합니다.
    /// </summary>
    public sealed partial class NBSAssetRegistry : SimpleAssetRegistry<NBSAssetHandle>
    {
        /// <inheritdoc cref="registryId" />
        public static Identifier id = new Identifier("runios", "nbs");

        /// <inheritdoc/>
        public override Identifier registryId => id;

        /// <inheritdoc/>
        public override RuniPath registryName => RuniPath.From("nbses");

        /// <inheritdoc/>
        public override int priority => 100;

        /// <inheritdoc/>
        public override Type assetType => typeof(NoteBlockClip);

        /// <inheritdoc/>
        public override IPatternMatcher assetMatcher => IPatternMatcher.nbsMatcher;

        [OnCodeLoaded]
        static void OnCodeLoaded() => AssetRegistryManager.Register<NBSAssetRegistry>();

        [OnCodeUnloading]
        static void OnCodeUnloading() => AssetRegistryManager.Unregister<NBSAssetRegistry>();

        protected override UniTask<NBSAssetHandle> CreateHandle(IONode node, FileMetaData fileMetaData, AssetImportData importData) => UniTask.FromResult(new NBSAssetHandle(node, fileMetaData, importData));
    }
}
