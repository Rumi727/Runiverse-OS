#nullable enable
using RuniOS.IO;
using System.IO;

namespace RuniOS.Utility
{
    public static class DirectoryUtility
    {
        public static void Copy(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);

            string[] files = Directory.GetFiles(sourceFolder);
            string[] folders = Directory.GetDirectories(sourceFolder);

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);

                File.Copy(file, dest);
            }

            for (int i = 0; i < folders.Length; i++)
            {
                string folder = folders[i];
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);

                Copy(folder, dest);
            }
        }

        public static string[] GetFiles(string path, WildcardPatterns extensionFilter) => EnumerateFiles(path, extensionFilter, SearchOption.TopDirectoryOnly).ToArray();
        public static string[] GetFiles(string path, WildcardPatterns extensionFilter, SearchOption searchOption) => EnumerateFiles(path, extensionFilter, searchOption).ToArray();

        public static IEnumerable<string> EnumerateFiles(string path, WildcardPatterns extensionFilter) => EnumerateFiles(path, extensionFilter, SearchOption.TopDirectoryOnly);
        public static IEnumerable<string> EnumerateFiles(string path, WildcardPatterns extensionFilter, SearchOption searchOption) => Directory.EnumerateFiles(path, "*", searchOption)
            .Where(extensionFilter.IsMatch);
    }
}