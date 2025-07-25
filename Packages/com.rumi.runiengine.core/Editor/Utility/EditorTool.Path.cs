#nullable enable
using System;
using System.IO;

namespace RuniEngine.Editor
{
    public partial class EditorTool
    {
        public static string AbsolutePathToRelativePath(string path) => path.Remove(Directory.GetCurrentDirectory().Length + 1);

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
