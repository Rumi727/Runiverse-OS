namespace RuniOS.Resource
{
    public interface IAssetRef
    {
        Type targetAssetType { get; }
        ResourceKey key { get; set; }
    }
}