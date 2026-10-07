#nullable enable
using RuniOS.Resource;
using UnityEngine.UI;

namespace RuniOS.UI
{
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("Asset Binders/Image Binder")]
    public sealed class ImageBinder : AssetScopeBinder<Sprite>
    {
        public Image? image => field = this.GetComponentFieldSave(field);

        protected override void Apply(Sprite asset)
        {
            if (image != null)
                image.sprite = asset;
        }

        protected override void ClearAsset()
        {
            if (image != null)
                image.sprite = null;
        }
    }
}