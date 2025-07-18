#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace RuniEngine.IO
{
    public readonly struct ExtensionFilter
    {
        public static ExtensionFilter allFileFilter { get; } = new ExtensionFilter("*");
        public static ExtensionFilter pictureFileFilter { get; } = new ExtensionFilter(".png", ".jpg", ".jif", ".jpeg", ".jpe", ".bmp", ".exr", ".gif", ".hdr", ".iff", ".pict", ".tif", ".tiff", ".psd", ".ico", ".jng", ".koa", ".lbm", ".mng", ".pbm", ".pcd", ".pcx", ".pgm", ".ppm", ".ras", ".tga", ".targa", ".wbpm", ".cut", ".xbm", ".xpm", ".dds", ".g3", ".sgi", ".j2k", ".j2c", ".jp2", ".pfm", ".webp", ".jxr");
        public static ExtensionFilter textFileFilter { get; } = new ExtensionFilter(".txt", ".html", ".htm", ".xml", ".bytes", ".json", ".csv", ".yaml", ".fnt");
        public static ExtensionFilter musicFileFilter { get; } = new ExtensionFilter(".ogg", ".mp3", ".mp2", ".wav", ".aif", ".xm", ".mod", ".it", ".vag", ".xma", ".s3m");
        public static ExtensionFilter nbsFileFilter { get; } = new ExtensionFilter(".nbs");
        public static ExtensionFilter videoFileFilter { get; } = new ExtensionFilter(".asf", ".avi", ".dv", ".m4v", ".mov", ".mp4", ".mpg", ".mpeg", ".ogv", ".vp8", ".webm", ".wmv");
        public static ExtensionFilter compressFileFilter { get; } = new ExtensionFilter(".zip");
        public static ExtensionFilter codeFileFilter { get; } = new ExtensionFilter(".java", ".php", ".scss", ".cs", ".css", ".js", ".py", ".c", ".cpp", ".class", ".fs", ".go", ".rb");
        public static ExtensionFilter jsonFileFilter { get; } = new ExtensionFilter(".json");



        public IReadOnlyList<string> extensions { get; }

        public ExtensionFilter(params string[] extensions) => this.extensions = extensions.ToArray().AsReadOnly();

        public static implicit operator ExtensionFilter(string value) => new ExtensionFilter(value.Split("|"));

        public override string ToString()
        {
            string result = "";
            for (int i = 0; i < extensions.Count; i++)
            {
                if (i < extensions.Count - 1)
                    result += extensions[i] + "|";
                else
                    result += extensions[i];
            }

            return result;
        }
    }
}
