namespace RuniOS.Resource
{
    public interface IAssetScope : IDisposable
    {
        public IAssetHandle handle { get; }
    }
}