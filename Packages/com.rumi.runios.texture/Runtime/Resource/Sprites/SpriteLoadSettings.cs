#nullable enable
namespace RuniOS.Resource.Sprites
{
    public readonly record struct SpriteLoadSettings
    (
        Rect? rect,
        Vector2 pivot,
        float pixelsPerUnit,
        uint extrude,
        SpriteMeshType meshType,
        Vector4 border,
        bool generateFallbackPhysicsShape,
        IReadOnlyList<SpriteLoadSettings.SecondaryTexture> secondaryTextures
    )
    {
        public readonly record struct SecondaryTexture(string name, Identifier identifier)
        {
            public string name
            {
                get => field ?? string.Empty;
                init;
            } = name;
        }

        public static readonly SpriteLoadSettings defaultValue = new SpriteLoadSettings
        (
            null,
            new Vector2(0.5f, 0.5f),
            100,
            0,
            SpriteMeshType.Tight,
            Vector4.zero ,
            false,
            []
        );
    }
}