#nullable enable
namespace RuniOS.Resource
{
    [Serializable]
    public record struct ResourceKey(Identifier registryId, Identifier assetId)
    {
        [SerializeField] public Identifier registryId = registryId;
        [SerializeField] public Identifier assetId = assetId;
    }
}