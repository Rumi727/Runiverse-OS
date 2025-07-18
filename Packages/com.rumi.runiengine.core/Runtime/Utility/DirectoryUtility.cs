#nullable enable
using RuniEngine.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RuniEngine
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

        public static string[] GetFiles(string path, ExtensionFilter extensionFilter) => EnumerateFiles(path, extensionFilter, SearchOption.TopDirectoryOnly).ToArray();
        public static string[] GetFiles(string path, ExtensionFilter extensionFilter, SearchOption searchOption) => EnumerateFiles(path, extensionFilter, searchOption).ToArray();

        public static IEnumerable<string> EnumerateFiles(string path, ExtensionFilter extensionFilter) => EnumerateFiles(path, extensionFilter, SearchOption.TopDirectoryOnly);
        public static IEnumerable<string> EnumerateFiles(string path, ExtensionFilter extensionFilter, SearchOption searchOption)
        {
            if (extensionFilter.extensions.Count == 1)
                return Directory.EnumerateFiles(path, "*" + extensionFilter.extensions[0], searchOption);

            return Directory.EnumerateFiles(path, "*", searchOption).Where(x =>
            {
                for (int i = 0; i < extensionFilter.extensions.Count; i++)
                {
                    if (x.EndsWith(extensionFilter.extensions[i]))
                        return true;
                }

                return false;
            });
        }
    }
}
