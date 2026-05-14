#nullable enable
using RuniOS.IO;
using System.IO;
using UnityEngine.Networking;

namespace RuniOS.Utility
{
    public static class PathUtility
    {
        /// <summary>
        /// The replacement character used when invalid name characters are converted to a safe character.<br/>
        /// 이름에 사용할 수 없는 문자를 안전한 문자로 변환할 때 사용하는 대체 문자를 나타냅니다.
        /// </summary>
        public const char alternativeNameChar = '_';

        /// <summary>
        /// The prefix used when converting a path value to a local file URL string.<br/>
        /// 경로 값을 로컬 파일 URL 문자열로 변환할 때 사용하는 접두사를 나타냅니다.
        /// </summary>
        public const string urlPathPrefix = "file:///";

        static readonly char[] invalidPathChars = Path.GetInvalidPathChars();
        static readonly char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Converts this path value to a local file URL string.<br/>
        /// 이 경로 값을 로컬 파일 URL 문자열로 변환합니다.
        /// </summary>
        /// <returns>
        /// A string prefixed with <see cref="urlPathPrefix"/> and escaped for URL usage.<br/>
        /// <see cref="urlPathPrefix"/>가 붙고 URL 용도로 이스케이프된 문자열을 반환합니다.
        /// </returns>
        public static string UrlPathPrefix(string path) => urlPathPrefix + UnityWebRequest.EscapeURL(path);

        /// <summary>
        /// 지정한 경로에서 시스템에서 정의한 잘못된 경로 문자(<see cref="Path.GetInvalidPathChars"/>)를 지정된 문자로 대체한 새 <see cref="RuniPath"/>를 반환합니다.<br/>
        /// 기본 대체 문자는 <see cref="alternativeNameChar"/> ('_')입니다.
        /// </summary>
        /// <param name="path">지정할 경로입니다.</param>
        /// <param name="newChar">잘못된 문자를 대체할 문자입니다. 기본값은 <see cref="alternativeNameChar"/>입니다.</param>
        /// <returns>잘못된 문자가 대체된 새 문자열입니다.</returns>
        public static string FixPathChars(string path, char newChar = alternativeNameChar)
        {
            if (string.IsNullOrEmpty(path))
                return RuniPath.empty;

            int lastPathIndex = path.LastIndexOfAny(RuniPath.directorySeparatorChars);
            if (lastPathIndex < 0) lastPathIndex = path.Length;

            ReadOnlySpan<char> pathPart = path.AsSpan(0, lastPathIndex);
            if (pathPart.IndexOfAny(invalidPathChars) < 0)
                return path;

            return string.Create(path.Length, (value: path, newChar, lastPathIndex), static (span, state) =>
            {
                state.value.AsSpan().CopyTo(span);
                for (int i = 0; i < state.lastPathIndex; i++)
                {
                    if (Array.IndexOf(invalidPathChars, span[i]) >= 0)
                        span[i] = state.newChar;
                }
            });
        }

        /// <summary>
        /// 지정한 경로의 파일 이름 부분에서 시스템에서 정의한 잘못된 파일 이름 문자(<see cref="System.IO.Path.GetInvalidFileNameChars"/>)를 지정된 문자로 대체한 새 <see cref="RuniPath"/>를 반환합니다.<br/>
        /// 기본 대체 문자는 <see cref="alternativeNameChar"/> ('_')입니다. 이 메서드는 경로 전체가 아닌 파일 이름 부분에만 적용됩니다.
        /// </summary>
        /// <param name="path">지정할 경로입니다.</param>
        /// <param name="newChar">잘못된 문자를 대체할 문자입니다. 기본값은 <see cref="alternativeNameChar"/>입니다.</param>
        /// <returns>잘못된 파일 이름 문자가 대체된 새 문자열입니다.</returns>
        public static string FixFileNameChars(string path, char newChar = alternativeNameChar)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            int lastPathIndex = path.LastIndexOfAny(RuniPath.directorySeparatorChars);
            ReadOnlySpan<char> filePart = path.AsSpan(lastPathIndex + 1);
            if (filePart.IndexOfAny(invalidFileNameChars) < 0)
                return path;

            return string.Create(path.Length, (path, newChar, lastPathIndex), static (span, state) =>
            {
                state.path.AsSpan().CopyTo(span);
                for (int i = state.lastPathIndex + 1; i < span.Length; i++)
                {
                    if (Array.IndexOf(invalidFileNameChars, span[i]) >= 0)
                        span[i] = state.newChar;
                }
            });
        }

        /// <summary>
        /// 파일 이름이 Windows 예약어(CON, PRN, AUX, NUL, COM1~9, LPT1~9)인지 확인합니다.
        /// </summary>
        public static bool IsWindowsReservedName(ReadOnlySpan<char> path)
        {
            ReadOnlySpan<char> name = Path.GetFileNameWithoutExtension(path);
            if (name.Length != 3 && name.Length != 4)
                return false;

            Span<char> upperName = stackalloc char[name.Length];
            for (int i = 0; i < name.Length; i++)
                upperName[i] = char.ToUpperInvariant(name[i]);

            if (upperName.Length == 3)
            {
                return upperName switch
                {
                    "CON" or "PRN" or "AUX" or "NUL" => true,
                    _ => false
                };
            }
            else if (upperName.StartsWith("COM") || upperName.StartsWith("LPT"))
            {
                char lastChar = upperName[3];
                return lastChar is >= '1' and <= '9'; // COM1~9, LPT1~9
            }

            return false;
        }

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