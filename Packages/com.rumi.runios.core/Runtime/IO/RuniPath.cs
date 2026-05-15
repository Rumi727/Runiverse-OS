#nullable enable
using Newtonsoft.Json;
using RuniOS.Json.Converters.IO;
using RuniOS.Spans;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.IO
{
    /// <summary>
    /// Represents a normalized logical path used by the framework.<br/>
    /// The path uses <see cref="directorySeparatorChar"/> internally and strips leading and trailing separators during normalization.
    /// <br/><br/>
    /// 프레임워크에서 사용하는 정규화된 논리 경로를 나타냅니다.<br/>
    /// 경로는 내부적으로 <see cref="directorySeparatorChar"/>를 사용하며, 정규화 과정에서 시작과 끝의 구분자를 제거합니다.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(RuniPathConverter))]
    public struct RuniPath : IEquatable<RuniPath>, ISerializationCallbackReceiver
    {
        /// <summary>
        /// The normalized directory separator character used by <see cref="RuniPath"/>.<br/>
        /// <see cref="RuniPath"/>가 정규화된 경로에 사용하는 디렉터리 구분 문자를 나타냅니다.
        /// </summary>
        public const char directorySeparatorChar = '/';

        /// <summary>
        /// The Windows directory separator character that is accepted during normalization.<br/>
        /// 정규화 과정에서 허용되는 Windows 디렉터리 구분 문자를 나타냅니다.
        /// </summary>
        public const char windowsDirectorySeparatorChar = '\\';

        /// <summary>
        /// The directory separator characters recognized by normalization.<br/>
        /// 정규화 과정에서 인식하는 디렉터리 구분 문자 컬렉션입니다.
        /// </summary>
        public static readonly char[] directorySeparatorChars = ['/', '\\'];

        /// <summary>
        /// The empty <see cref="RuniPath"/> value.<br/>
        /// 빈 <see cref="RuniPath"/> 값을 나타냅니다.
        /// </summary>
        public static readonly RuniPath empty = new RuniPath();



        /// <summary>
        /// Gets or sets the normalized string value of this path.<br/>
        /// Assigned values are normalized through <see cref="NormalizePath(string)"/>.
        /// <br/><br/>
        /// 이 경로의 정규화된 문자열 값을 가져오거나 설정합니다.<br/>
        /// 설정된 값은 <see cref="NormalizePath(string)"/>를 통해 정규화됩니다.
        /// </summary>
        [AllowNull]
        public string value
        {
            readonly get => _value ?? string.Empty;
            set => _value = NormalizePath(value ?? string.Empty);
        }
        [SerializeField, FieldName("gui.value"), NotNullField, JsonIgnore] string? _value;

        /// <summary>
        /// Gets the length of the normalized path string.<br/>
        /// 정규화된 경로 문자열의 길이를 가져옵니다.
        /// </summary>
        public readonly int length => _value?.Length ?? 0;



        /// <summary>
        /// Initializes a new <see cref="RuniPath"/> from the specified path string.<br/>
        /// 지정된 경로 문자열에서 새 <see cref="RuniPath"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to normalize.<br/>
        /// 정규화할 경로 문자열입니다.
        /// </param>
        public RuniPath(string path) => _value = NormalizePath(path);

        /// <summary>
        /// Initializes a new <see cref="RuniPath"/> from the specified path string.<br/>
        /// 지정된 경로 문자열에서 새 <see cref="RuniPath"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="path">
        /// The path span to normalize.<br/>
        /// 정규화할 경로 span입니다.
        /// </param>
        public RuniPath(ReadOnlySpan<char> path) => _value = NormalizePath(path.ToString());



        /// <summary>
        /// Creates a new <see cref="RuniPath"/> from the specified path string.<br/>
        /// 지정된 경로 문자열에서 새 <see cref="RuniPath"/>를 생성합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to normalize.<br/>
        /// 정규화할 경로 문자열입니다.
        /// </param>
        /// <returns>
        /// A normalized <see cref="RuniPath"/> value.<br/>
        /// 정규화된 <see cref="RuniPath"/> 값을 반환합니다.
        /// </returns>
        public static RuniPath From(string path) => new RuniPath(path);

        /// <summary>
        /// Creates a new <see cref="RuniPath"/> from the specified path string.<br/>
        /// 지정된 경로 문자열에서 새 <see cref="RuniPath"/>를 생성합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to normalize.<br/>
        /// 정규화할 경로 문자열입니다.
        /// </param>
        /// <returns>
        /// A normalized <see cref="RuniPath"/> value.<br/>
        /// 정규화된 <see cref="RuniPath"/> 값을 반환합니다.
        /// </returns>
        public static RuniPath From(ReadOnlySpan<char> path) => new RuniPath(path);



        /// <summary>
        /// Gets the extension portion of the last path segment.<br/>
        /// 마지막 경로 세그먼트의 확장자 부분을 가져옵니다.
        /// </summary>
        /// <returns>
        /// The extension represented by the last path segment.<br/>
        /// 마지막 경로 세그먼트에서 얻은 확장자를 반환합니다.
        /// </returns>
        public readonly FileExtension GetExtension() => new FileExtension(this);

        /// <summary>
        /// Gets the last segment of this path.<br/>
        /// 이 경로의 마지막 세그먼트를 가져옵니다.
        /// </summary>
        /// <returns>
        /// The text after the last <see cref="directorySeparatorChar"/>, or the whole path when it has no separator.<br/>
        /// 마지막 <see cref="directorySeparatorChar"/> 뒤의 문자열을 반환하며, 구분자가 없으면 전체 경로를 반환합니다.
        /// </returns>
        public readonly string GetFileName() => PathUtility.GetFileName(value).ToString();

        /// <summary>
        /// Gets the last path segment without its extension.<br/>
        /// 마지막 경로 세그먼트에서 확장자를 제외한 값을 가져옵니다.
        /// </summary>
        /// <returns>
        /// The last path segment without its extension, or the full segment when no extension exists.<br/>
        /// 확장자를 제외한 마지막 경로 세그먼트를 반환하며, 확장자가 없으면 세그먼트 전체를 반환합니다.
        /// </returns>
        public readonly string GetFileNameWithoutExtension() => PathUtility.GetFileNameWithoutExtension(value).ToString();

        /// <summary>
        /// Gets a path with the extension removed from the last segment.<br/>
        /// 마지막 세그먼트의 확장자를 제거한 경로를 가져옵니다.
        /// </summary>
        /// <returns>
        /// A path without the last segment extension, or this path when no extension exists.<br/>
        /// 마지막 세그먼트의 확장자가 제거된 경로를 반환하며, 확장자가 없으면 현재 경로를 반환합니다.
        /// </returns>
        public readonly RuniPath GetPathWithoutExtension() => new RuniPath(PathUtility.GetPathWithoutExtension(value));

        /// <summary>
        /// Gets the path that contains every segment except the last one.<br/>
        /// 마지막 세그먼트를 제외한 나머지 경로를 가져옵니다.
        /// </summary>
        /// <returns>
        /// The parent path, or <see cref="empty"/> when this path has no parent segment.<br/>
        /// 상위 경로를 반환하며, 상위 세그먼트가 없으면 <see cref="empty"/>를 반환합니다.
        /// </returns>
        public readonly RuniPath GetParentPath() => new RuniPath(PathUtility.GetParentPath(value));



        /// <summary>
        /// Removes the specified prefix path when this path is under it.<br/>
        /// 이 경로가 지정된 접두사 경로 아래에 있으면 해당 접두사를 제거합니다.
        /// </summary>
        /// <param name="relativeTo">
        /// The prefix path to remove.<br/>
        /// 제거할 접두사 경로입니다.
        /// </param>
        /// <returns>
        /// The trimmed path when the prefix matches; otherwise, this path.<br/>
        /// 접두사가 일치하면 제거된 경로를 반환하고, 그렇지 않으면 현재 경로를 반환합니다.
        /// </returns>
        public readonly RuniPath TrimStartPath(RuniPath relativeTo) => new RuniPath(PathUtility.TrimStartPath(value, relativeTo.value));

        /// <summary>
        /// Attempts to remove the specified prefix path from this path.<br/>
        /// 이 경로에서 지정된 접두사 경로 제거를 시도합니다.
        /// </summary>
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
        public readonly bool TryTrimStartPath(RuniPath relativeTo, out RuniPath result)
        {
            bool success = PathUtility.TryTrimStartPath(value, relativeTo.value, out ReadOnlySpan<char> span);
            result = new RuniPath(span);
            return success;
        }



        /// <summary>
        /// Determines whether this path starts with the specified prefix path on a segment boundary.<br/>
        /// 이 경로가 지정된 접두사 경로로 시작하며 세그먼트 경계가 일치하는지 확인합니다.
        /// </summary>
        /// <param name="startPath">
        /// The prefix path to compare.<br/>
        /// 비교할 접두사 경로입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if this path equals <paramref name="startPath"/> or is under it; otherwise, <see langword="false"/>.<br/>
        /// 이 경로가 <paramref name="startPath"/>와 같거나 그 아래에 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public readonly bool StartsWith(RuniPath startPath) => PathUtility.StartsWith(value, startPath.value);



        /// <summary>
        /// Determines whether this path has an empty value.<br/>
        /// 이 경로가 빈 값을 가지는지 확인합니다.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the path is empty; otherwise, <see langword="false"/>.<br/>
        /// 경로가 비어 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public readonly bool IsEmpty() => string.IsNullOrEmpty(_value);



        /// <summary>
        /// Appends the specified extension to this path.<br/>
        /// 이 경로에 지정된 확장자를 덧붙입니다.
        /// </summary>
        /// <param name="ext">
        /// The extension to append.<br/>
        /// 덧붙일 확장자입니다.
        /// </param>
        /// <returns>
        /// A new <see cref="RuniPath"/> with the extension appended.<br/>
        /// 확장자가 덧붙여진 새 <see cref="RuniPath"/>를 반환합니다.
        /// </returns>
        public readonly RuniPath AddExtension(FileExtension ext) => new RuniPath(value + ext);

        /// <summary>
        /// Appends the specified extension to this path.<br/>
        /// 이 경로에 지정된 확장자를 덧붙입니다.
        /// </summary>
        /// <param name="ext">
        /// The extension to append.<br/>
        /// 덧붙일 확장자입니다.
        /// </param>
        /// <returns>
        /// A new <see cref="RuniPath"/> with the extension appended.<br/>
        /// 확장자가 덧붙여진 새 <see cref="RuniPath"/>를 반환합니다.
        /// </returns>
        public readonly RuniPath AddExtension(string ext) => new RuniPath(value + (FileExtension)ext);



        /// <summary>
        /// Combines this path with another logical path.<br/>
        /// 이 경로에 다른 논리 경로를 결합합니다.
        /// </summary>
        /// <param name="path">
        /// The logical path to append.<br/>
        /// 덧붙일 논리 경로입니다.
        /// </param>
        /// <returns>
        /// The combined path, or the non-empty path when either side is empty.<br/>
        /// 결합된 경로를 반환하며, 한쪽 경로가 비어 있으면 비어 있지 않은 경로를 반환합니다.
        /// </returns>
        public readonly RuniPath Combine(RuniPath path) => (RuniPath)PathUtility.CombineFromNormalizedPath(value, path.value);

        /// <summary>
        /// Combines this path with another path string treated as a logical relative path.<br/>
        /// 이 경로에 논리 상대 경로로 취급되는 다른 경로 문자열을 결합합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to normalize and append.<br/>
        /// 정규화한 뒤 덧붙일 경로 문자열입니다.
        /// </param>
        /// <returns>
        /// The combined <see cref="RuniPath"/>.<br/>
        /// 결합된 <see cref="RuniPath"/>를 반환합니다.
        /// </returns>
        public readonly RuniPath Combine(string path) => (RuniPath)PathUtility.CombineFromNormalizedPath(value, NormalizePath(path));



        /// <summary>
        /// Normalizes a path string into the <see cref="RuniPath"/> text format.<br/>
        /// The result uses <see cref="directorySeparatorChar"/>, removes leading and trailing separators, collapses repeated separators, and rejects traversal segments.
        /// <br/><br/>
        /// 경로 문자열을 <see cref="RuniPath"/> 텍스트 형식으로 정규화합니다.<br/>
        /// 결과는 <see cref="directorySeparatorChar"/>를 사용하고, 시작과 끝의 구분자를 제거하며, 반복된 구분자를 합치고, 경로 이동 세그먼트를 거부합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to normalize.<br/>
        /// 정규화할 경로 문자열입니다.
        /// </param>
        /// <returns>
        /// The normalized path string, or <see cref="string.Empty"/> when <paramref name="path"/> has no usable segment.<br/>
        /// 정규화된 경로 문자열을 반환하며, <paramref name="path"/>에 사용할 수 있는 세그먼트가 없으면 <see cref="string.Empty"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="path"/> contains <c>.</c> or <c>..</c> as a path segment.<br/>
        /// <paramref name="path"/>가 경로 세그먼트로 <c>.</c> 또는 <c>..</c>를 포함하는 경우 발생합니다.
        /// </exception>
        public static string NormalizePath(string path) // TODO : 나중에 유니티 닷넷 올라가면 Span으로 바꿀 것 (string.Create에 allows ref struct 붙어있어서 사용 가능)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            int length = CalculateNormalizeLength(path.AsSpan());
            if (length == 0)
                return string.Empty;

            return string.Create(length, path, static (span, path) =>
            {
                int dst = 0;
                foreach (var item in path.AsSpan().Trim(directorySeparatorChars).SplitAny(directorySeparatorChars))
                {
                    if (item.IsEmpty)
                        continue;

                    if (dst > 0)
                    {
                        span[dst] = directorySeparatorChar;
                        dst++;
                    }

                    item.CopyTo(span.Slice(dst));
                    dst += item.Length;
                }
            });
        }

        public static bool IsNormalized(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            return path.Length == CalculateNormalizeLength(path.AsSpan());
        }

        static int CalculateNormalizeLength(ReadOnlySpan<char> path)
        {
            if (path.IsEmpty)
                return 0;

            path = path.Trim(directorySeparatorChars);
            if (path.IsEmpty)
                return 0;

            int length = 0;
            foreach (var item in path.SplitAny(directorySeparatorChars))
            {
                if (item.IsEmpty)
                    continue;

                switch (item)
                {
                    case ".":
                        throw new ArgumentException("Directory traversal ('.') is not allowed for security reasons.");
                    case "..":
                        throw new ArgumentException("Directory traversal ('..') is not allowed for security reasons.");
                }

                length += item.Length + 1;
            }

            return length - 1;
        }



        /// <summary>
        /// Returns the normalized string value of this path.<br/>
        /// 이 경로의 정규화된 문자열 값을 반환합니다.
        /// </summary>
        /// <returns>
        /// The value of <see cref="value"/>.<br/>
        /// <see cref="value"/> 값을 반환합니다.
        /// </returns>
        public override readonly string ToString() => value;



        #region Equals
        /// <summary>
        /// Determines whether the specified object is equal to this path.<br/>
        /// 지정된 객체가 이 경로와 같은지 확인합니다.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with this path.<br/>
        /// 이 경로와 비교할 객체입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is an equal <see cref="RuniPath"/>; otherwise, <see langword="false"/>.<br/>
        /// <paramref name="obj"/>가 같은 <see cref="RuniPath"/>이면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public override readonly bool Equals(object? obj) => obj is RuniPath path && Equals(path);

        /// <summary>
        /// Determines whether the specified path is equal to this path.<br/>
        /// 지정된 경로가 이 경로와 같은지 확인합니다.
        /// </summary>
        /// <param name="other">
        /// The path to compare with this path.<br/>
        /// 이 경로와 비교할 경로입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if both paths have the same <see cref="value"/>; otherwise, <see langword="false"/>.<br/>
        /// 두 경로의 <see cref="value"/>가 같으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public readonly bool Equals(RuniPath other) => value == other.value;
        #endregion



        /// <summary>
        /// Returns the hash code for this path.<br/>
        /// 이 경로의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>
        /// The hash code of <see cref="value"/>.<br/>
        /// <see cref="value"/>의 해시 코드를 반환합니다.
        /// </returns>
        public override readonly int GetHashCode() => value.GetHashCode();



        /// <summary>
        /// Deconstructs this path into its parent path and last segment.<br/>
        /// 이 경로를 상위 경로와 마지막 세그먼트로 분해합니다.
        /// </summary>
        /// <param name="directory">
        /// The parent path.<br/>
        /// 상위 경로입니다.
        /// </param>
        /// <param name="name">
        /// The last path segment.<br/>
        /// 마지막 경로 세그먼트입니다.
        /// </param>
        public void Deconstruct(out RuniPath directory, out string name)
        {
            directory = GetParentPath();
            name = GetFileName();
        }
        
        /// <summary>
        /// Deconstructs this path into its parent path, last segment without extension, and extension.<br/>
        /// 이 경로를 상위 경로, 확장자를 제외한 마지막 세그먼트, 확장자로 분해합니다.
        /// </summary>
        /// <param name="directory">
        /// The parent path.<br/>
        /// 상위 경로입니다.
        /// </param>
        /// <param name="name">
        /// The last path segment without its extension.<br/>
        /// 확장자를 제외한 마지막 경로 세그먼트입니다.
        /// </param>
        /// <param name="extension">
        /// The extension of the last path segment.<br/>
        /// 마지막 경로 세그먼트의 확장자입니다.
        /// </param>
        public void Deconstruct(out RuniPath directory, out string name, out FileExtension extension)
        {
            directory = GetParentPath();
            name = GetFileNameWithoutExtension();
            extension = GetExtension();
        }




        #region operators
        /// <summary>
        /// Converts a <see cref="RuniPath"/> to its normalized string value.<br/>
        /// <see cref="RuniPath"/>를 정규화된 문자열 값으로 변환합니다.
        /// </summary>
        /// <param name="path">
        /// The path to convert.<br/>
        /// 변환할 경로입니다.
        /// </param>
        public static explicit operator string(RuniPath path) => path.value;

        /// <summary>
        /// Converts a string to a normalized <see cref="RuniPath"/>.<br/>
        /// 문자열을 정규화된 <see cref="RuniPath"/>로 변환합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to convert.<br/>
        /// 변환할 경로 문자열입니다.
        /// </param>
        public static explicit operator RuniPath(string path) => new RuniPath(path);

        /// <summary>
        /// Converts a string to a normalized <see cref="RuniPath"/>.<br/>
        /// 문자열을 정규화된 <see cref="RuniPath"/>로 변환합니다.
        /// </summary>
        /// <param name="path">
        /// The path span to convert.<br/>
        /// 변환할 경로 span입니다.
        /// </param>
        public static explicit operator RuniPath(ReadOnlySpan<char> path) => new RuniPath(path);



        /// <summary>
        /// Determines whether two paths are equal.<br/>
        /// 두 경로가 같은지 확인합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to compare.<br/>
        /// 비교할 첫 번째 경로입니다.
        /// </param>
        /// <param name="right">
        /// The second path to compare.<br/>
        /// 비교할 두 번째 경로입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the paths are equal; otherwise, <see langword="false"/>.<br/>
        /// 두 경로가 같으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator ==(RuniPath left, RuniPath right) => left.Equals(right);

        /// <summary>
        /// Determines whether two paths are equal.<br/>
        /// 두 경로가 같은지 확인합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to compare.<br/>
        /// 비교할 첫 번째 경로입니다.
        /// </param>
        /// <param name="right">
        /// The second path to compare.<br/>
        /// 비교할 두 번째 경로입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the paths are equal; otherwise, <see langword="false"/>.<br/>
        /// 두 경로가 같으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator ==(RuniPath left, string right) => left.value.Equals(right);

        /// <summary>
        /// Determines whether two paths are equal.<br/>
        /// 두 경로가 같은지 확인합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to compare.<br/>
        /// 비교할 첫 번째 경로입니다.
        /// </param>
        /// <param name="right">
        /// The second path to compare.<br/>
        /// 비교할 두 번째 경로입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the paths are equal; otherwise, <see langword="false"/>.<br/>
        /// 두 경로가 같으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator ==(string left, RuniPath right) => left.Equals(right.value);

        /// <summary>
        /// Determines whether two paths are not equal.<br/>
        /// 두 경로가 같지 않은지 확인합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to compare.<br/>
        /// 비교할 첫 번째 경로입니다.
        /// </param>
        /// <param name="right">
        /// The second path to compare.<br/>
        /// 비교할 두 번째 경로입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the paths are not equal; otherwise, <see langword="false"/>.<br/>
        /// 두 경로가 같지 않으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator !=(RuniPath left, RuniPath right) => !(left == right);

        /// <summary>
        /// Determines whether two paths are not equal.<br/>
        /// 두 경로가 같지 않은지 확인합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to compare.<br/>
        /// 비교할 첫 번째 경로입니다.
        /// </param>
        /// <param name="right">
        /// The second path to compare.<br/>
        /// 비교할 두 번째 경로입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the paths are not equal; otherwise, <see langword="false"/>.<br/>
        /// 두 경로가 같지 않으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator !=(RuniPath left, string right) => !(left == right);

        /// <summary>
        /// Determines whether two paths are not equal.<br/>
        /// 두 경로가 같지 않은지 확인합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to compare.<br/>
        /// 비교할 첫 번째 경로입니다.
        /// </param>
        /// <param name="right">
        /// The second path to compare.<br/>
        /// 비교할 두 번째 경로입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the paths are not equal; otherwise, <see langword="false"/>.<br/>
        /// 두 경로가 같지 않으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator !=(string left, RuniPath right) => !(left == right);
        #endregion



        void ISerializationCallbackReceiver.OnBeforeSerialize() => value = value;
        void ISerializationCallbackReceiver.OnAfterDeserialize() => value = value;
    }
}
