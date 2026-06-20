#nullable enable
using RuniOS.IO;
using System.IO;
using UnityEditor.Build;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace RuniOS.Editor
{
    public sealed class PackageStreamingAssetsBuildProcessor : BuildPlayerProcessor
    {
        public override int callbackOrder => -1000;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            HashSet<string> addedPaths = new(StringComparer.Ordinal);
            foreach (PackageInfo packageInfo in PackageInfo.GetAllRegisteredPackages())
            {
                string sourceRoot = Path.Combine(packageInfo.resolvedPath, StreamingIOProvider.streamingAssetsFolderName);
                if (!Directory.Exists(sourceRoot))
                    continue;

                AddStreamingAssets(buildPlayerContext, sourceRoot, addedPaths);
            }
        }

        static void AddStreamingAssets(BuildPlayerContext buildPlayerContext, string sourceRoot, HashSet<string> addedPaths)
        {
            foreach (string sourceFile in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                if (sourceFile.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string relativePath = Path.GetRelativePath(sourceRoot, sourceFile).Replace('\\', '/');
                if (!addedPaths.Add(relativePath))
                    continue;

                string projectPath = Path.Combine(Application.streamingAssetsPath, relativePath);
                if (File.Exists(projectPath) || Directory.Exists(projectPath))
                    continue;

                buildPlayerContext.AddAdditionalPathToStreamingAssets(sourceFile, relativePath);
            }
        }
    }
}
