#nullable enable
using RuniEngine.IO;

namespace RuniEngine
{
    public static class PathUtility
    {
        public static FilePath ToPath(this string? path) => path;

        /*public const char directorySeparatorChar = '/';
        public const char alternativeNameChar = '_';
        public const string urlPathPrefix = "file:///";

        public static readonly char[] directorySeparatorChars = new char[] { '/', '\\' };

        public static string RemoveInvalidPathChars(string filename) => string.Concat(filename.Split(System.IO.Path.GetInvalidPathChars()));
        public static string ReplaceInvalidPathChars(string filename, char newChar = alternativeNameChar) => string.Join(newChar, filename.Split(System.IO.Path.GetInvalidPathChars()));

        public static string RemoveInvalidFileNameChars(string filename) => string.Concat(filename.Split(System.IO.Path.GetInvalidFileNameChars()));
        public static string ReplaceInvalidFileNameChars(string filename, char newChar = alternativeNameChar) => string.Join(newChar, filename.Split(System.IO.Path.GetInvalidFileNameChars()));

        public static string GetExtension(string path)
        {
            int index = path.LastIndexOf('.');
            if (index < 0)
                return string.Empty;
            
            return path.Substring(index);
        }

        public static string GetFileName(string path)
        {
            int index = path.LastIndexOfAny(directorySeparatorChars);
            if (index < 0)
                return path;

            return path.Substring(index + 1);
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            string fileName = GetFileName(path);
            int extIndex = fileName.LastIndexOf('.');

            if (extIndex < 0)
                return fileName;
            else
                return fileName.Remove(extIndex);
        }

        public static string GetPathWithoutExtension(string path)
        {
            int extIndex = path.LastIndexOf('.');
            if (extIndex < 0)
                return path;
            else
                return path.Remove(extIndex);
        }

        public static string GetParentPath(string path)
        {
            int index = path.LastIndexOfAny(directorySeparatorChars);
            if (index < 0)
                return string.Empty;

            return path.Substring(0, index);
        }

        public static string UrlPathPrefix(this string path) => urlPathPrefix + UnityWebRequest.EscapeURL(path);

        public static string NormalizeSeparators(this string path) => path.NormalizeSeparators('\\', directorySeparatorChar);
        public static string NormalizeSeparators(this string path, char altSeparatorChar, char separatorChar) => path.Replace(altSeparatorChar, separatorChar);

        public static string Combine(params string?[] paths)
        {
            StringBuilder stringBuilder = StringBuilderCache.Acquire();
            for (int i = 0; i < paths.Length; i++)
            {
                string? path = paths[i];
                if (path == null || path.Length <= 0)
                    continue;

                path = path.NormalizeSeparators();

                if (stringBuilder.Length <= 0)
                {
                    stringBuilder.Append(path);
                    continue;
                }

                char last = stringBuilder[stringBuilder.Length - 1];
                if (last != directorySeparatorChar)
                    stringBuilder.Append(directorySeparatorChar);

                stringBuilder.Append(path);
            }

            return StringBuilderCache.Release(stringBuilder);
        }

        public static string GetRelativePath(string relativeTo, string path)
        {
            relativeTo = relativeTo.NormalizeSeparators();
            path = path.NormalizeSeparators();

            if (relativeTo.Length <= 0)
                return path;

            if (path.Length <= 0 || relativeTo == path)
                return string.Empty;

            if (path.StartsWith(relativeTo))
            {
                path = path.Substring(relativeTo.Length);
                if (path.Length > 0 && path[0] == directorySeparatorChar)
                    path = path.Substring(1);
            }

            return path;
        }

        public static bool StartsWith(string path, string startPath)
        {
            string[] paths = path.Split(directorySeparatorChars);
            string[] startPaths = startPath.Split(directorySeparatorChars);

            if (paths.Length < startPaths.Length)
                return false;

            for (int i = 0; i < startPaths.Length; i++)
            {
                if (paths[i] != startPaths[i])
                    return false;
            }

            return true;
        }*/
    }
}
