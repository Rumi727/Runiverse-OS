#nullable enable
namespace RuniOS.IO
{
    public interface IPatternMatcher
    {
        public static readonly IPatternMatcher allMatcher = new AllPatternMatcher();
        public static readonly IPatternMatcher pictureMatcher = PatternMatcherSet.CreateExt(".png", ".jpg", ".jif", ".jpeg", ".jpe", ".bmp", ".exr", ".gif", ".hdr", ".iff", ".pict", ".tif", ".tiff", ".psd", ".ico", ".jng", ".koa", ".lbm", ".mng", ".pbm", ".pcd", ".pcx", ".pgm", ".ppm", ".ras", ".tga", ".targa", ".wbpm", ".cut", ".xbm", ".xpm", ".dds", ".g3", ".sgi", ".j2k", ".j2c", ".jp2", ".pfm", ".webp", ".jxr");
        public static readonly IPatternMatcher textMatcher = PatternMatcherSet.CreateExt(".txt", ".html", ".htm", ".xml", ".bytes", ".json", ".csv", ".yaml", ".fnt");
        public static readonly IPatternMatcher musicMatcher = PatternMatcherSet.CreateExt(".ogg", ".mp3", ".mp2", ".wav", ".aif", ".xm", ".mod", ".it", ".vag", ".xma", ".s3m");
        public static readonly IPatternMatcher nbsMatcher = new ExtensionMatcher(".nbs");
        public static readonly IPatternMatcher videoMatcher = PatternMatcherSet.CreateExt(".asf", ".avi", ".dv", ".m4v", ".mov", ".mp4", ".mpg", ".mpeg", ".ogv", ".vp8", ".webm", ".wmv");
        public static readonly IPatternMatcher compressMatcher = new ExtensionMatcher(".zip");
        public static readonly IPatternMatcher codeMatcher = PatternMatcherSet.CreateExt(".java", ".php", ".scss", ".cs", ".css", ".js", ".py", ".c", ".cpp", ".class", ".fs", ".go", ".rb");
        public static readonly IPatternMatcher jsonMatcher = new ExtensionMatcher(".json");

        bool IsMatch(scoped ReadOnlySpan<char> path);
        bool IsMatch(string path) => IsMatch(path.AsSpan());
        bool IsMatch(RuniPath path) => IsMatch(path.value.AsSpan());
    }
}