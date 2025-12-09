#nullable enable
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

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

        public WildcardPatterns(string pattern) => patterns = ImmutableArray.Create(pattern);
        public WildcardPatterns(params string[] patterns) => this.patterns = patterns.ToImmutableArray();
        public WildcardPatterns(IEnumerable<string> patterns) => this.patterns = patterns.ToImmutableArray();
        public WildcardPatterns(ImmutableArray<string> patterns) => this.patterns = patterns;

        public string this[int index] => patterns[index];

        public int count => patterns.Length;
        int IReadOnlyCollection<string>.Count => count;
        
        ImmutableArray<string> patterns { get; }

        public ImmutableArray<string>.Enumerator GetEnumerator() => patterns.GetEnumerator();
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => ((IEnumerable<string>)patterns).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)patterns).GetEnumerator();

        static readonly ConcurrentDictionary<string, Regex> _regexCache = new();

        public bool IsMatch(string text) => IsMatch(text, this);
        public bool IsMatch(string text, bool ignoreCase) => IsMatch(text, this, ignoreCase);
        
        public bool IsMatch(FilePath text) => IsMatch(text, this);
        public bool IsMatch(FilePath path, bool ignoreCase) => IsMatch(path, this, ignoreCase);

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
            // 캐시 키 생성 (패턴 + 대소문자 옵션)
            string cacheKey = pattern + (ignoreCase ? ":i" : ":s");

            // 캐시에 있으면 가져오고, 없으면 새로 생성 (스레드 안전)
            Regex regex = _regexCache.GetOrAdd(cacheKey, _ =>
            {
                string regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                RegexOptions options = RegexOptions.Compiled | RegexOptions.ExplicitCapture;
                if (ignoreCase)
                    options |= RegexOptions.IgnoreCase;

                return new Regex(regexPattern, options);
            });

            return regex.IsMatch(text);
        }

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