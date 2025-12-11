#nullable enable
using RuniOS.IO;
using System.IO;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static FilePath projectPath { get; } = Directory.GetCurrentDirectory();

        public static bool PathIsProjectPath(string path)
        {
            path = path.Replace("\\", "/");
            string projectPath = Directory.GetCurrentDirectory();

            if (path.StartsWith(Path.Combine(projectPath, "Assets").Replace("\\", "/"), StringComparison.Ordinal))
                return true;
            else if (path.StartsWith(Path.Combine(projectPath, "Packages").Replace("\\", "/"), StringComparison.Ordinal))
                return true;

            return false;
        }
    }
}