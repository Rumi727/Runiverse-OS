#nullable enable
namespace RuniOS.Localizations
{
    sealed class LocalizationData(IReadOnlyDictionary<string, string> localizations)
    {
        public IReadOnlyDictionary<string, string> localizations { get; } = localizations;
    }
}