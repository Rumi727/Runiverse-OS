#nullable enable
using RuniOS.IO;
using System.IO;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static PhysicalPath projectPath { get; } = (PhysicalPath)Directory.GetCurrentDirectory();

        public static bool IsProjectPath(PhysicalPath path) => path.StartsWith(projectPath / "Assets") || path.StartsWith(projectPath / "Packages");
    }
}