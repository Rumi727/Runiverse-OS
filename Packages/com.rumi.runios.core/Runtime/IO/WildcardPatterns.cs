#nullable enable
using System.Collections;
using System.Collections.Immutable;

namespace RuniOS.IO
{
    public readonly struct WildcardPatterns : IReadOnlyList<string>
    {
        public static WildcardPatterns allFileFilter { get; } = new WildcardPatterns("*");
        public static WildcardPatterns pictureFileFilter { get; } = new WildcardPatterns("*.png", "*.jpg", "*.jif", "*.jpeg", "*.jpe", "*.bmp", "*.exr", "*.gif", "*.hdr", "*.iff", "*.pict", "*.tif", "*.tiff", "*.psd", "*.ico", "*.jng", "*.koa", "*.lbm", "*.mng", "*.pbm", "*.pcd", "*.pcx", "*.pgm", "*.ppm", "*.ras", "*.tga", "*.targa", "*.wbpm", "*.cut", "*.xbm", "*.xpm", "*.dds", "*.g3", "*.sgi", "*.j2k", "*.j2c", "*.jp2", "*.pfm", "*.webp", "*.jxr");
        public static WildcardPatterns textFileFilter { get; } = new WildcardPatterns("*.txt", "*.html", "*.htm", "*.xml", "*.bytes", "*.json", "*.csv", "*.yaml", "*.fnt");
        public static WildcardPatterns musicFileFilter { get; } = new WildcardPatterns("*.ogg", "*.mp3", "*.mp2", "*.wav", "*.aif", "*.xm", "*.mod", "*.it", "*.vag", "*.xma", "*.s3m");
        public static WildcardPatterns nbsFileFilter { get; } = new WildcardPatterns("*.nbs");
        public static WildcardPatterns videoFileFilter { get; } = new WildcardPatterns("*.asf", "*.avi", "*.dv", "*.m4v", "*.mov", "*.mp4", "*.mpg", "*.mpeg", "*.ogv", "*.vp8", "*.webm", "*.wmv");
        public static WildcardPatterns compressFileFilter { get; } = new WildcardPatterns("*.zip");
        public static WildcardPatterns codeFileFilter { get; } = new WildcardPatterns("*.java", "*.php", "*.scss", "*.cs", "*.css", "*.js", "*.py", "*.c", "*.cpp", "*.class", "*.fs", "*.go", "*.rb");
        public static WildcardPatterns jsonFileFilter { get; } = new WildcardPatterns("*.json");

        public WildcardPatterns(string pattern) => patterns = [pattern];
        public WildcardPatterns(params string[] patterns) => this.patterns = [..patterns];
        public WildcardPatterns(IEnumerable<string> patterns) => this.patterns = [..patterns];
        public WildcardPatterns(ImmutableArray<string> patterns) => this.patterns = patterns;

        public string this[int index] => patterns[index];

        public int count => patterns.Length;
        int IReadOnlyCollection<string>.Count => count;
        
        ImmutableArray<string> patterns { get; }

        public ImmutableArray<string>.Enumerator GetEnumerator() => patterns.GetEnumerator();
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => ((IEnumerable<string>)patterns).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)patterns).GetEnumerator();

        public bool IsMatch(string text) => IsMatch(text, this);
        public bool IsMatch(string text, bool ignoreCase) => IsMatch(text, this, ignoreCase);
        
        public bool IsMatch(RuniPath text) => IsMatch(text.value, this);
        public bool IsMatch(RuniPath path, bool ignoreCase) => IsMatch(path.value, this, ignoreCase);

        /// <summary>
        /// 와일드카드 패턴에 따라 문자열이 일치하는지 확인합니다.
        /// '*'는 0개 이상의 문자와 일치하고, '?'는 1개의 문자와 일치합니다.
        /// </summary>
        /// <param name="text">검색할 문자열.</param>
        /// <param name="pattern">와일드카드 패턴.</param>
        /// <param name="ignoreCase">대소문자를 무시할지 여부.</param>
        /// <returns>문자열이 패턴과 일치하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>.</returns>
        public static bool IsMatch(string text, string pattern, bool ignoreCase = false)
        {
            int textIndex = 0;
            int patternIndex = 0;
            int starIndex = -1;
            int starTextIndex = -1;

            while (textIndex < text.Length)
            {
                if (patternIndex < pattern.Length &&
                    pattern[patternIndex] != '*' &&
                    (pattern[patternIndex] == '?' || AreEqual(text[textIndex], pattern[patternIndex], ignoreCase)))
                {
                    textIndex++;
                    patternIndex++;
                    continue;
                }

                if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
                {
                    starIndex = patternIndex++;
                    starTextIndex = textIndex;
                    continue;
                }

                if (starIndex >= 0)
                {
                    patternIndex = starIndex + 1;
                    textIndex = ++starTextIndex;
                    continue;
                }

                return false;
            }

            while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
                patternIndex++;

            return patternIndex == pattern.Length;
        }

        static bool AreEqual(char textCharacter, char patternCharacter, bool ignoreCase) =>
            !ignoreCase || textCharacter == patternCharacter || char.ToUpperInvariant(textCharacter) == char.ToUpperInvariant(patternCharacter);

        /// <summary>
        /// 와일드카드 패턴에 따라 문자열이 일치하는지 확인합니다.
        /// '*'는 0개 이상의 문자와 일치하고, '?'는 1개의 문자와 일치합니다.
        /// </summary>
        /// <param name="text">검색할 문자열.</param>
        /// <param name="patterns">와일드카드 패턴.</param>
        /// <param name="ignoreCase">대소문자를 무시할지 여부.</param>
        /// <returns>문자열이 패턴과 일치하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>.</returns>
        public static bool IsMatch(string text, WildcardPatterns patterns, bool ignoreCase = false)
        {
            for (int i = 0; i < patterns.patterns.Length; i++)
            {
                if (IsMatch(text, patterns[i], ignoreCase))
                    return true;
            }

            return false;
        }

        public static implicit operator WildcardPatterns(string value) => new WildcardPatterns(value);
    }
}
