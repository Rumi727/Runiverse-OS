#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Tasks;
using UnityEngine;

namespace RuniOS.Resource
{
    [ExecuteAlways]
    public abstract class AssetScopeBinder<TAsset> : MonoBehaviour, IReloadable where TAsset : notnull
    {
        public AssetRef<TAsset> assetRef
        {
            get => _assetRef;
            set
            {
                if (_assetRef == value)
                    return;

                _assetRef = value;
                Reload().Forget();
            }
        }
        [SerializeField] AssetRef<TAsset> _assetRef;

        public IAssetScope<TAsset>? currentAssetScope { get; private set; }

        protected virtual void OnEnable()
        {
            ResourceManager.AttachReloadable(this);
            Reload().Forget();
        }

        protected void OnDisable()
        {
            ResourceManager.DetachReloadable(this);

            ClearAsset();
            currentAssetScope?.Dispose();
        }

        readonly AsyncReloadGate asyncReloadGate = new AsyncReloadGate();
        public UniTask Reload() => asyncReloadGate.Run(ReloadCore);

        async UniTask ReloadCore()
        {
            if (assetRef.IsSameTarget(currentAssetScope))
                return;

            currentAssetScope = await assetRef.LoadScopeAsync();
            if (currentAssetScope != null)
                Apply(currentAssetScope.asset);
            else
                ClearAsset();
        }

        protected abstract void Apply(TAsset asset);
        protected abstract void ClearAsset();
    }
}