#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Resource;
using RuniOS.Tasks;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    public class AssetScopeManipulator<TAsset> : ReloadableManipulator where TAsset : notnull
    {
        public AssetScopeManipulator() : this(default(AssetRef<TAsset>)) { }
        public AssetScopeManipulator(Identifier assetId) : this(new AssetRef<TAsset>(assetId)) { }
        public AssetScopeManipulator(Identifier registryId, Identifier assetId) : this(new AssetRef<TAsset>(registryId, assetId)) { }
        public AssetScopeManipulator(ResourceKey resourceKey) : this(new AssetRef<TAsset>(resourceKey)) { }
        public AssetScopeManipulator(TAsset asset) : this(new AssetRef<TAsset>(asset)) { }
        public AssetScopeManipulator(AssetRef<TAsset> assetRef)
        {
            this.assetRef = assetRef;
#if UNITY_EDITOR
            editorThemeAwareManipulator = new EditorThemeAwareManipulator(OnThemeChanged);
#endif
        }

        public AssetRef<TAsset> assetRef
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Reload().Forget();
            }
        }

        public AssetRef<TAsset>? editorAssetRef
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Reload().Forget();
            }
        }

        AssetRef<TAsset> effectiveAssetRef
        {
            get
            {
#if UNITY_EDITOR
                if (isEditorTheme)
                    return editorAssetRef ?? assetRef;
#endif
                return assetRef;
            }
        }

        public IAssetScope<TAsset>? currentAssetScope { get; private set; }

        public required Action<TAsset> applyAsset { private get; init; }
        public required Action clearAsset { private get; init; }

#if UNITY_EDITOR
        readonly EditorThemeAwareManipulator editorThemeAwareManipulator;
        bool isEditorTheme;

        void OnThemeChanged(bool isEditorTheme)
        {
            if (this.isEditorTheme == isEditorTheme)
                return;

            this.isEditorTheme = isEditorTheme;
            Reload().Forget();
        }
#endif

        protected override void RegisterCallbacksOnTarget()
        {
#if UNITY_EDITOR
            target.AddManipulator(editorThemeAwareManipulator);
#endif
            base.RegisterCallbacksOnTarget();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
#if UNITY_EDITOR
            target.RemoveManipulator(editorThemeAwareManipulator);
#endif
            base.UnregisterCallbacksFromTarget();
        }

        protected override void OnDetach(DetachFromPanelEvent evt)
        {
            base.OnDetach(evt);

            ClearAsset();
            currentAssetScope?.Dispose();
            currentAssetScope = null;
        }

        readonly AsyncReloadGate asyncReloadGate = new();
        public override UniTask Reload() => UniTask.WhenAll(base.Reload(), asyncReloadGate.Run(ReloadCore));

        async UniTask ReloadCore()
        {
            await base.Reload();

            VisualElement? reloadTarget = target;
            if (reloadTarget?.panel == null)
                return;

            AssetRef<TAsset> loadRef = effectiveAssetRef;
            if (loadRef.IsSameTarget(currentAssetScope))
                return;

            IAssetScope<TAsset>? newScope = await loadRef.LoadScopeAsync();
            if (target != reloadTarget || reloadTarget.panel == null || loadRef != effectiveAssetRef)
            {
                newScope?.Dispose();
                return;
            }

            currentAssetScope?.Dispose();
            currentAssetScope = newScope;

            if (newScope != null)
                Apply(newScope.asset);
            else
                ClearAsset();
        }

        protected virtual void Apply(TAsset asset) => applyAsset.Invoke(asset);
        protected virtual void ClearAsset() => clearAsset.Invoke();
    }
}