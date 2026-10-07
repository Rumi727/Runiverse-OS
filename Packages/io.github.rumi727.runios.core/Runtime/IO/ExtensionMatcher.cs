#nullable enable
namespace RuniOS.IO
{
    public readonly struct ExtensionMatcher(FileExtension extension) : IPatternMatcher
    {
        public ExtensionMatcher(string extension) : this(new FileExtension(extension)) { }

        public FileExtension extension { get; } = extension;

        public bool IsMatch(scoped ReadOnlySpan<char> path) => RuniPathUtility.GetExtension(path).SequenceEqual(extension.value);

        public static implicit operator ExtensionMatcher(string extension) => new ExtensionMatcher((FileExtension)extension);
    }
}
