#nullable enable
namespace RuniOS.Resource
{
    public interface IAssetImportSettings
    {
        object? value { get; }

        bool IsSameTarget(IAssetImportSettings other);
    }
}