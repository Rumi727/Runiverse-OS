#nullable enable
using RuniOS.IO;
using System.IO;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static FilePath projectPath { get; } = Directory.GetCurrentDirectory();

        public static bool IsProjectPath(FilePath path) => path.StartsWith(projectPath + "Assets") || path.StartsWith(projectPath + "Packages");
    }
}