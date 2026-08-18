#nullable enable
using RuniOS.IO;
using System.IO;
using UnityEngine.Networking;

namespace RuniOS.Utility
{
    /// <summary>
    /// Provides shared helpers for normalized path strings used by <see cref="RuniPath"/> and related path types.<br/>
    /// <see cref="RuniPath"/> 및 관련 경로 타입에서 사용하는 정규화된 경로 문자열 헬퍼를 제공합니다.
    /// </summary>
    public static class RuniPathUtility
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
        /// <param name="path">
        /// The path string to convert.<br/>
        /// 변환할 경로 문자열입니다.
        /// </param>
        /// <returns>
        /// A string prefixed with <see cref="urlPathPrefix"/> and escaped for URL usage.<br/>
        /// <see cref="urlPathPrefix"/>가 붙고 URL 용도로 이스케이프된 문자열을 반환합니다.
        /// </returns>
        public static string UrlPathPrefix(string path) => urlPathPrefix + UnityWebRequest.EscapeURL(path);

        /// <summary>
        /// Replaces invalid path characters in the directory portion of the specified path string.<br/>
        /// 지정된 경로 문자열의 디렉터리 부분에서 잘못된 경로 문자를 대체합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to sanitize.<br/>
        /// 정리할 경로 문자열입니다.
        /// </param>
        /// <param name="newChar">
        /// The replacement character used for invalid path characters.<br/>
        /// 잘못된 경로 문자를 대체할 문자입니다.
        /// </param>
        /// <returns>
        /// A string with invalid path characters replaced, or the original string when no replacement is needed.<br/>
        /// 잘못된 경로 문자가 대체된 문자열을 반환하며, 대체가 필요 없으면 원본 문자열을 반환합니다.
        /// </returns>
        public static string FixPathChars(string path, char newChar = alternativeNameChar)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            int lastPathIndex = path.LastIndexOf(RuniPath.directorySeparatorChar);
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
        /// Replaces invalid file-name characters in the last segment of the specified path string.<br/>
        /// 지정된 경로 문자열의 마지막 세그먼트에서 잘못된 파일 이름 문자를 대체합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to sanitize.<br/>
        /// 정리할 경로 문자열입니다.
        /// </param>
        /// <param name="newChar">
        /// The replacement character used for invalid file-name characters.<br/>
        /// 잘못된 파일 이름 문자를 대체할 문자입니다.
        /// </param>
        /// <returns>
        /// A string with invalid file-name characters replaced, or the original string when no replacement is needed.<br/>
        /// 잘못된 파일 이름 문자가 대체된 문자열을 반환하며, 대체가 필요 없으면 원본 문자열을 반환합니다.
        /// </returns>
        public static string FixFileNameChars(string path, char newChar = alternativeNameChar)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            int lastPathIndex = path.LastIndexOf(RuniPath.directorySeparatorChar);
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
        /// Determines whether the file name portion of the specified path is a Windows reserved device name.<br/>
        /// 지정된 경로의 파일 이름 부분이 Windows 예약 장치 이름인지 확인합니다.
        /// </summary>
        /// <param name="path">
        /// The path or file name to inspect.<br/>
        /// 검사할 경로 또는 파일 이름입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the file name is a Windows reserved device name; otherwise, <see langword="false"/>.<br/>
        /// 파일 이름이 Windows 예약 장치 이름이면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool IsWindowsReservedName(scoped ReadOnlySpan<char> path)
        {
            ReadOnlySpan<char> name = GetFileNameWithoutExtension(path);
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


        /// <summary>
        /// Gets the extension from the last segment of the specified path string.<br/>
        /// 지정된 경로 문자열의 마지막 세그먼트에서 확장자를 가져옵니다.
        /// </summary>
        /// <param name="path">
        /// The path string to inspect.<br/>
        /// 검사할 경로 문자열입니다.
        /// </param>
        /// <returns>
        /// The extension represented by the last segment of <paramref name="path"/>.<br/>
        /// <paramref name="path"/>의 마지막 세그먼트에서 얻은 확장자를 반환합니다.
        /// </returns>
        public static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
        {
            if (path.IsEmpty)
                return ReadOnlySpan<char>.Empty;

            int separatorIndex = path.LastIndexOf(RuniPath.directorySeparatorChar);
            int extIndex = path.LastIndexOf(FileExtension.extensionSeparatorChar);

            if (extIndex <= separatorIndex)
                return ReadOnlySpan<char>.Empty;

            return path.Slice(extIndex);
        }



        /// <summary>
        /// Gets the last segment of the specified normalized path span.<br/>
        /// 지정된 정규화 경로 span의 마지막 세그먼트를 가져옵니다.
        /// </summary>
        /// <param name="path">
        /// The path span to inspect.<br/>
        /// 검사할 경로 span입니다.
        /// </param>
        /// <returns>
        /// The text after the last <see cref="RuniPath.directorySeparatorChar"/>, or the whole span when it has no separator.<br/>
        /// 마지막 <see cref="RuniPath.directorySeparatorChar"/> 뒤의 문자열을 반환하며, 구분자가 없으면 전체 span을 반환합니다.
        /// </returns>
        public static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path)
        {
            int index = path.LastIndexOf(RuniPath.directorySeparatorChar);
            if (index < 0)
                return path;

            return path.Slice(index + 1);
        }

        /// <summary>
        /// Gets the last path segment without its extension.<br/>
        /// 마지막 경로 세그먼트에서 확장자를 제외한 값을 가져옵니다.
        /// </summary>
        /// <param name="path">
        /// The path span to inspect.<br/>
        /// 검사할 경로 span입니다.
        /// </param>
        /// <returns>
        /// The last path segment without its extension, or the full segment when no extension exists.<br/>
        /// 확장자를 제외한 마지막 경로 세그먼트를 반환하며, 확장자가 없으면 세그먼트 전체를 반환합니다.
        /// </returns>
        public static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path)
        {
            ReadOnlySpan<char> fileName = GetFileName(path);
            int extIndex = fileName.LastIndexOf(FileExtension.extensionSeparatorChar);

            if (extIndex < 0)
                return fileName;
            else
                return fileName.Slice(0, extIndex);
        }


        /// <summary>
        /// Gets the specified path without the extension of its last segment.<br/>
        /// 지정된 경로에서 마지막 세그먼트의 확장자를 제거한 값을 가져옵니다.
        /// </summary>
        /// <param name="path">
        /// The path span to inspect.<br/>
        /// 검사할 경로 span입니다.
        /// </param>
        /// <returns>
        /// A span without the last segment extension, or the original span when no extension exists.<br/>
        /// 마지막 세그먼트의 확장자가 제거된 span을 반환하며, 확장자가 없으면 원본 span을 반환합니다.
        /// </returns>
        public static ReadOnlySpan<char> GetPathWithoutExtension(ReadOnlySpan<char> path)
        {
            int separatorIndex = path.LastIndexOf(RuniPath.directorySeparatorChar);
            int extIndex = path.LastIndexOf(FileExtension.extensionSeparatorChar);

            if (extIndex <= separatorIndex)
                return path;

            if (extIndex == separatorIndex + 1)
                return separatorIndex < 0 ? ReadOnlySpan<char>.Empty : path.Slice(0, separatorIndex);

            return path.Slice(0, extIndex);
        }


        /// <summary>
        /// Gets the parent path of the specified normalized path span.<br/>
        /// 지정된 정규화 경로 span의 상위 경로를 가져옵니다.
        /// </summary>
        /// <param name="path">
        /// The path span to inspect.<br/>
        /// 검사할 경로 span입니다.
        /// </param>
        /// <returns>
        /// The parent path span, or an empty span when the path has no parent segment.<br/>
        /// 상위 경로 span을 반환하며, 상위 세그먼트가 없으면 빈 span을 반환합니다.
        /// </returns>
        public static ReadOnlySpan<char> GetParentPath(ReadOnlySpan<char> path)
        {
            int index = path.LastIndexOf(RuniPath.directorySeparatorChar);
            if (index < 0)
                return ReadOnlySpan<char>.Empty;

            return path.Slice(0, index);
        }


        /// <summary>
        /// Removes the specified prefix path when the path is under it.<br/>
        /// 경로가 지정된 접두사 경로 아래에 있으면 해당 접두사를 제거합니다.
        /// </summary>
        /// <param name="path">
        /// The path span to trim.<br/>
        /// 접두사를 제거할 경로 span입니다.
        /// </param>
        /// <param name="relativeTo">
        /// The prefix path to remove.<br/>
        /// 제거할 접두사 경로입니다.
        /// </param>
        /// <returns>
        /// The trimmed path when the prefix matches; otherwise, <paramref name="path"/>.<br/>
        /// 접두사가 일치하면 제거된 경로를 반환하고, 그렇지 않으면 빈 경로를 반환합니다.
        /// </returns>
        public static ReadOnlySpan<char> GetRelativePath(ReadOnlySpan<char> path, scoped ReadOnlySpan<char> relativeTo)
        {
            if (TryGetRelativePath(path, relativeTo, out var result))
                return result;

            return ReadOnlySpan<char>.Empty;
        }

        /// <summary>
        /// Attempts to remove the specified prefix path from the path span.<br/>
        /// 경로 span에서 지정된 접두사 경로 제거를 시도합니다.
        /// </summary>
        /// <param name="path">
        /// The path span to trim.<br/>
        /// 접두사를 제거할 경로 span입니다.
        /// </param>
        /// <param name="relativeTo">
        /// The prefix path to remove.<br/>
        /// 제거할 접두사 경로입니다.
        /// </param>
        /// <param name="result">
        /// When this method returns <see langword="true"/>, contains the trimmed path.<br/>
        /// 이 메서드가 <see langword="true"/>를 반환하면 접두사가 제거된 경로를 포함합니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the prefix matches; otherwise, <see langword="false"/>.<br/>
        /// 접두사가 일치하면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool TryGetRelativePath(ReadOnlySpan<char> path, scoped ReadOnlySpan<char> relativeTo, out ReadOnlySpan<char> result)
        {
            if (path == relativeTo)
            {
                result = ReadOnlySpan<char>.Empty;
                return true;
            }

            if (StartsWith(path, relativeTo))
            {
                result = path.Slice(relativeTo.Length + 1);
                return true;
            }

            result = ReadOnlySpan<char>.Empty;
            return false;
        }

        /// <summary>
        /// Determines whether the path starts with the specified prefix on a segment boundary.<br/>
        /// 경로가 지정된 접두사로 시작하며 세그먼트 경계가 일치하는지 확인합니다.
        /// </summary>
        /// <param name="path">
        /// The path span to inspect.<br/>
        /// 검사할 경로 span입니다.
        /// </param>
        /// <param name="startPath">
        /// The prefix path to compare.<br/>
        /// 비교할 접두사 경로입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="path"/> equals <paramref name="startPath"/> or is under it; otherwise, <see langword="false"/>.<br/>
        /// <paramref name="path"/>가 <paramref name="startPath"/>와 같거나 그 아래에 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool StartsWith(scoped ReadOnlySpan<char> path, scoped ReadOnlySpan<char> startPath)
        {
            if (path == startPath)
                return true;
            if (path.Length <= startPath.Length)
                return false;

            // 접두사 뒤에 구분자가 있어야 "folder_A"가 "folder"의 하위 경로로 판정되지 않습니다.
            return path[startPath.Length] == RuniPath.directorySeparatorChar && path.StartsWith(startPath, StringComparison.Ordinal);
        }
    }
}
