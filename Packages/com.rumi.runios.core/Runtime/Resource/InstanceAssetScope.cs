#nullable enable
namespace RuniOS.Resource
{
    public sealed class InstanceAssetScope<TAsset> : IAssetScope<TAsset>
    {
        internal InstanceAssetScope(IAssetHandle<TAsset> handle, TAsset asset)
        {
            this.handle = handle;
            this.asset = asset;
        }
        
        public IAssetHandle<TAsset> handle { get; }
        public TAsset asset { get; }
        
        public void Dispose() { }
        
        public static implicit operator TAsset(InstanceAssetScope<TAsset> scope) => scope.asset;
    }
}