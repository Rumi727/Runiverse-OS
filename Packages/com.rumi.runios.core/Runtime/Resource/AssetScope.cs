#nullable enable
using System;

namespace RuniOS.Resource
{
    public abstract class AssetScope : IDisposable
    {
        public AssetHandle handle { get; }

        protected AssetScope(AssetHandle handle) => this.handle = handle;
        
        public void Dispose()
        {
            try
            {
                handle.ReturnScope(this);
                GC.SuppressFinalize(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        ~AssetScope() => Debug.ForceLogError(
            $"AssetScope for handle '{handle.ioHandler.fullPath}' was finalized without being properly disposed.\n" +
            "This is likely a resource leak. Ensure 'Dispose()' or 'using' is used to dispose this IDisposable asset."
        );
    }
}
