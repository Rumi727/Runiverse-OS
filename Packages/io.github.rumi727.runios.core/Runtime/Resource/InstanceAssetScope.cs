#nullable enable
namespace RuniOS.Resource
{
    public record struct InstanceAssetScope<TAsset> : IAssetScope<TAsset> where TAsset : notnull
    {
        internal InstanceAssetScope(IAssetHandle<TAsset> handle, TAsset asset)
        {
            this.handle = handle;
            this.asset = asset;
        }

        public IAssetHandle<TAsset> handle { get; }
        public TAsset asset { get; }

        void IDisposable.Dispose() { }

        public static implicit operator TAsset(InstanceAssetScope<TAsset> scope) => scope.asset;
    }
}