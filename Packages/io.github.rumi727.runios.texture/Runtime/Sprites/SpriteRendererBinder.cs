using RuniOS.Resource;

namespace RuniOS.Sprites
{
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Asset Binders/Sprite Renderer Binder")]
    public sealed class SpriteRendererBinder : AssetScopeBinder<Sprite>
    {
        public SpriteRenderer? spriteRenderer => field = this.GetComponentFieldSave(field);

        protected override void Apply(Sprite asset)
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = asset;
        }

        protected override void ClearAsset()
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = null;
        }
    }
}