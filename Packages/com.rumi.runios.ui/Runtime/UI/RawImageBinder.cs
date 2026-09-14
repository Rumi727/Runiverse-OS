#nullable enable
using RuniOS.Resource;
using UnityEngine.UI;

namespace RuniOS.UI
{
    [RequireComponent(typeof(RawImage))]
    [AddComponentMenu("Asset Binders/Raw Image Binder")]
    public sealed class RawImageBinder : AssetScopeBinder<Texture2D>
    {
        public RawImage? rawImage => field = this.GetComponentFieldSave(field);

        protected override void Apply(Texture2D asset)
        {
            if (rawImage != null)
                rawImage.texture = asset;
        }

        protected override void ClearAsset()
        {
            if (rawImage != null)
                rawImage.texture = null;
        }
    }
}