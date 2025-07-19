#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace RuniEngine.IO
{
    public readonly struct WildcardPatterns
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



        public IReadOnlyList<string> patterns { get; }

        public WildcardPatterns(string pattern) => patterns = new string[] { pattern };
        public WildcardPatterns(params string[] patterns) => this.patterns = patterns.ToArray().AsReadOnly();

        public string this[int index] => patterns[index];

        public static implicit operator WildcardPatterns(string value) => new WildcardPatterns(value);

        public override string ToString()
        {
            string result = "";
            for (int i = 0; i < patterns.Count; i++)
            {
                if (i < patterns.Count - 1)
                    result += patterns[i] + "|";
                else
                    result += patterns[i];
            }

            return result;
        }
    }
}
