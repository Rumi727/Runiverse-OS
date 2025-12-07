#nullable enable
namespace RuniOS.Localizations
{
    sealed class LocalizationData
    {
        public LocalizationData(IReadOnlyDictionary<string, string> localizations) => this.localizations = localizations;
        
        public IReadOnlyDictionary<string, string> localizations { get; }
    }
}