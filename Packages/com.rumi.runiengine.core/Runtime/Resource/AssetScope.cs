#nullable enable
using System;

namespace RuniEngine.Resource
{
    public abstract class AssetScope : IDisposable
    {
        public AssetHandle handle { get; }

        protected AssetScope(AssetHandle handle) => this.handle = handle;



        public void Dispose() => handle.ReturnScope(this);

        ~AssetScope() => handle.ReturnScope(this);
    }
}
