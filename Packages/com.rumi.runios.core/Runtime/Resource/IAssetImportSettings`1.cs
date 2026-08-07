#nullable enable
namespace RuniOS.Resource
{
    public interface IAssetImportSettings<out T> : IAssetImportSettings
    {
        new T? value { get; }
        object? IAssetImportSettings.value => value;
    }
}