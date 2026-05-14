#nullable enable
using RuniOS.IO;
using System.IO;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static RuniPath projectPath { get; } = Directory.GetCurrentDirectory();

        public static bool IsProjectPath(RuniPath path) => path.StartsWith(projectPath + "Assets") || path.StartsWith(projectPath + "Packages");
    }
}