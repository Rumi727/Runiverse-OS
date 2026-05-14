#nullable enable
using Newtonsoft.Json;
using RuniOS.Json.Converters.IO;
using RuniOS.Spans;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

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
        /// The path string to normalize. It may be <see langword="null"/>.<br/>
        /// 정규화할 경로 문자열입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        public RuniPath(string? path) => _value = NormalizePath(path ?? string.Empty);

        /// <summary>
        /// Initializes a new <see cref="RuniPath"/> by combining two path segments.<br/>
        /// 두 경로 세그먼트를 결합하여 새 <see cref="RuniPath"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="path1">
        /// The first path segment. It may be <see langword="null"/>.<br/>
        /// 첫 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="path2">
        /// The second path segment. It may be <see langword="null"/>.<br/>
        /// 두 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        public RuniPath(string? path1, string? path2) => _value = NormalizePath(path1 + directorySeparatorChar + path2);

        /// <summary>
        /// Initializes a new <see cref="RuniPath"/> by combining three path segments.<br/>
        /// 세 경로 세그먼트를 결합하여 새 <see cref="RuniPath"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="path1">
        /// The first path segment. It may be <see langword="null"/>.<br/>
        /// 첫 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="path2">
        /// The second path segment. It may be <see langword="null"/>.<br/>
        /// 두 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="path3">
        /// The third path segment. It may be <see langword="null"/>.<br/>
        /// 세 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        public RuniPath(string? path1, string? path2, string? path3) => _value = NormalizePath(path1 + directorySeparatorChar + path2 + directorySeparatorChar + path3);

        /// <summary>
        /// Initializes a new <see cref="RuniPath"/> by combining four path segments.<br/>
        /// 네 경로 세그먼트를 결합하여 새 <see cref="RuniPath"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="path1">
        /// The first path segment. It may be <see langword="null"/>.<br/>
        /// 첫 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="path2">
        /// The second path segment. It may be <see langword="null"/>.<br/>
        /// 두 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="path3">
        /// The third path segment. It may be <see langword="null"/>.<br/>
        /// 세 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="path4">
        /// The fourth path segment. It may be <see langword="null"/>.<br/>
        /// 네 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        public RuniPath(string? path1, string? path2, string? path3, string? path4) => _value = NormalizePath(path1 + directorySeparatorChar + path2 + directorySeparatorChar + path3 + directorySeparatorChar + path4);

        /// <summary>
        /// Initializes a new <see cref="RuniPath"/> by combining five path segments.<br/>
        /// 다섯 경로 세그먼트를 결합하여 새 <see cref="RuniPath"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="path1">
        /// The first path segment. It may be <see langword="null"/>.<br/>
        /// 첫 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="path2">
        /// The second path segment. It may be <see langword="null"/>.<br/>
        /// 두 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="path3">
        /// The third path segment. It may be <see langword="null"/>.<br/>
        /// 세 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="path4">
        /// The fourth path segment. It may be <see langword="null"/>.<br/>
        /// 네 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="path5">
        /// The fifth path segment. It may be <see langword="null"/>.<br/>
        /// 다섯 번째 경로 세그먼트입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        public RuniPath(string? path1, string? path2, string? path3, string? path4, string? path5) => _value = NormalizePath(path1 + directorySeparatorChar + path2 + directorySeparatorChar + path3 + directorySeparatorChar + path4 + directorySeparatorChar + path5);

        /// <summary>
        /// Initializes a new <see cref="RuniPath"/> by joining the specified path segments.<br/>
        /// 지정된 경로 세그먼트들을 결합하여 새 <see cref="RuniPath"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="paths">
        /// The path segments to join.<br/>
        /// 결합할 경로 세그먼트 컬렉션입니다.
        /// </param>
        public RuniPath(params string[] paths) => _value = NormalizePath(string.Join(directorySeparatorChar, paths));

        /*/// <summary>
        /// 지정된 <see cref="ReadOnlySpan{T}"/> 경로로부터 새 <see cref="RuniPath"/> 인스턴스를 생성하고 정규화합니다.<br/>
        /// 입력된 경로는 <see cref="NormalizePath(string)"/>를 통해 표준 형식으로 변환됩니다.
        /// </summary>
        /// <param name="path">생성할 파일 경로를 나타내는 <see cref="ReadOnlySpan{T}"/>입니다.</param>
        /// <returns>정규화된 새 <see cref="RuniPath"/> 인스턴스입니다. 입력이 비어있으면 빈 경로를 나타내는 <see cref="empty"/> 인스턴스가 반환됩니다.</returns>
        public RuniPath(ReadOnlySpan<char> path) => _value = NormalizePath(path);*/



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
        public readonly string GetFileName()
        {
            int index = value.LastIndexOf(directorySeparatorChar);
            if (index < 0)
                return value;

            return value.Substring(index + 1);
        }

        /// <summary>
        /// Gets the last path segment without its extension.<br/>
        /// 마지막 경로 세그먼트에서 확장자를 제외한 값을 가져옵니다.
        /// </summary>
        /// <returns>
        /// The last path segment without its extension, or the full segment when no extension exists.<br/>
        /// 확장자를 제외한 마지막 경로 세그먼트를 반환하며, 확장자가 없으면 세그먼트 전체를 반환합니다.
        /// </returns>
        public readonly string GetFileNameWithoutExtension()
        {
            string fileName = GetFileName();
            int extIndex = fileName.LastIndexOf(FileExtension.extensionSeparatorChar);

            if (extIndex < 0)
                return fileName;
            else
                return fileName.Remove(extIndex);
        }

        /// <summary>
        /// Gets a path with the extension removed from the last segment.<br/>
        /// 마지막 세그먼트의 확장자를 제거한 경로를 가져옵니다.
        /// </summary>
        /// <returns>
        /// A path without the last segment extension, or this path when no extension exists.<br/>
        /// 마지막 세그먼트의 확장자가 제거된 경로를 반환하며, 확장자가 없으면 현재 경로를 반환합니다.
        /// </returns>
        public readonly RuniPath GetPathWithoutExtension()
        {
            int extIndex = value.LastIndexOf(FileExtension.extensionSeparatorChar);
            if (extIndex < 0)
                return _value;
            else
                return value.Remove(extIndex);
        }

        /// <summary>
        /// Gets the path that contains every segment except the last one.<br/>
        /// 마지막 세그먼트를 제외한 나머지 경로를 가져옵니다.
        /// </summary>
        /// <returns>
        /// The parent path, or <see cref="empty"/> when this path has no parent segment.<br/>
        /// 상위 경로를 반환하며, 상위 세그먼트가 없으면 <see cref="empty"/>를 반환합니다.
        /// </returns>
        public readonly RuniPath GetParentPath()
        {
            int index = value.LastIndexOf(directorySeparatorChar);
            if (index < 0)
                return string.Empty;

            return value.Substring(0, index);
        }




        /// <summary>
        /// Removes the specified prefix path when this path is under it.<br/>
        /// 이 경로가 지정된 접두사 경로 아래에 있으면 해당 접두사를 제거합니다.
        /// </summary>
        /// <param name="relativeTo">
        /// The prefix path to remove. It may be <see langword="null"/>.<br/>
        /// 제거할 접두사 경로입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <returns>
        /// The trimmed path when the prefix matches; otherwise, this path.<br/>
        /// 접두사가 일치하면 제거된 경로를 반환하고, 그렇지 않으면 현재 경로를 반환합니다.
        /// </returns>
        public readonly RuniPath TrimStartPath(RuniPath? relativeTo) => string.IsNullOrEmpty(relativeTo?.value) ? this : TrimStartPath(relativeTo.Value);

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
        public readonly RuniPath TrimStartPath(RuniPath relativeTo)
        {
            if (TryTrimStartPath(relativeTo, out RuniPath result))
                return result;

            return this;
        }

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
            if (relativeTo == this)
            {
                result = empty;
                return true;
            }

            if (StartsWith(relativeTo))
            {
                result = value.Substring(relativeTo.value.Length + 1);
                return true;
            }

            result = value;
            return false;
        }



        /// <summary>
        /// Determines whether this path starts with the specified prefix path.<br/>
        /// 이 경로가 지정된 접두사 경로로 시작하는지 확인합니다.
        /// </summary>
        /// <param name="startPath">
        /// The prefix path to compare. It may be <see langword="null"/>.<br/>
        /// 비교할 접두사 경로입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the prefix matches or <paramref name="startPath"/> is empty; otherwise, <see langword="false"/>.<br/>
        /// 접두사가 일치하거나 <paramref name="startPath"/>가 비어 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public readonly bool StartsWith(RuniPath? startPath) => string.IsNullOrEmpty(startPath?.value) || StartsWith(startPath);

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
        public readonly bool StartsWith(RuniPath startPath)
        {
            if (value == startPath)
                return true;
            if (length <= startPath.length)
                return false;
            
            return value[startPath.length] == directorySeparatorChar && value.StartsWith(startPath.value, StringComparison.Ordinal);
        }



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
        /// Combines two nullable path values into one normalized path.<br/>
        /// 두 nullable 경로 값을 하나의 정규화된 경로로 결합합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to combine. It may be <see langword="null"/>.<br/>
        /// 결합할 첫 번째 경로입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="right">
        /// The second path to combine. It may be <see langword="null"/>.<br/>
        /// 결합할 두 번째 경로입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <returns>
        /// The combined path, or <see cref="empty"/> when both paths are empty.<br/>
        /// 결합된 경로를 반환하며, 두 경로가 모두 비어 있으면 <see cref="empty"/>를 반환합니다.
        /// </returns>
        public static RuniPath Combine(RuniPath? left, RuniPath? right) => Combine(left ?? empty, right ?? empty);

        /// <summary>
        /// Combines two path values into one normalized path.<br/>
        /// 두 경로 값을 하나의 정규화된 경로로 결합합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to combine.<br/>
        /// 결합할 첫 번째 경로입니다.
        /// </param>
        /// <param name="right">
        /// The second path to combine.<br/>
        /// 결합할 두 번째 경로입니다.
        /// </param>
        /// <returns>
        /// The combined path, or the non-empty operand when one operand is empty.<br/>
        /// 결합된 경로를 반환하며, 한쪽 경로가 비어 있으면 비어 있지 않은 피연산자를 반환합니다.
        /// </returns>
        public static RuniPath Combine(RuniPath left, RuniPath right)
        {
            if (left.value.Length == 0 && right.value.Length == 0)
                return empty;
            else if (left.value.Length == 0) 
                return right;
            else if (right.value.Length == 0) 
                return left;

            return string.Create(left.value.Length + 1 + right.value.Length, (left: left.value, right: right.value), static (span, state) =>
            {
                int index = 0;
                for (int i = 0; i < state.left.Length; i++)
                {
                    span[index] = state.left[i];
                    index++;
                }

                span[index] = directorySeparatorChar;
                index++;

                for (int i = 0; i < state.right.Length; i++)
                {
                    span[index] = state.right[i];
                    index++;
                }
            });
        }



        /// <summary>
        /// Normalizes a path string into the <see cref="RuniPath"/> text format.<br/>
        /// The result uses <see cref="directorySeparatorChar"/>, removes leading and trailing separators, collapses repeated separators, and rejects traversal segments.
        /// <br/><br/>
        /// 경로 문자열을 <see cref="RuniPath"/> 텍스트 형식으로 정규화합니다.<br/>
        /// 결과는 <see cref="directorySeparatorChar"/>를 사용하고, 시작과 끝의 구분자를 제거하며, 반복된 구분자를 합치고, 경로 이동 세그먼트를 거부합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to normalize. It may be <see langword="null"/>.<br/>
        /// 정규화할 경로 문자열입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <returns>
        /// The normalized path string, or <see cref="string.Empty"/> when <paramref name="path"/> has no usable segment.<br/>
        /// 정규화된 경로 문자열을 반환하며, <paramref name="path"/>에 사용할 수 있는 세그먼트가 없으면 <see cref="string.Empty"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="path"/> contains <c>.</c> or <c>..</c> as a path segment.<br/>
        /// <paramref name="path"/>가 경로 세그먼트로 <c>.</c> 또는 <c>..</c>를 포함하는 경우 발생합니다.
        /// </exception>
        public static string NormalizePath(string? path) // TODO : 나중에 유니티 닷넷 올라가면 Span으로 바꿀 것 (string.Create에 allows ref struct 붙어있어서 사용 가능)
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
        /// Filters the specified paths by matching their values against wildcard patterns.<br/>
        /// 지정된 경로들을 와일드카드 패턴과 비교하여 필터링합니다.
        /// </summary>
        /// <param name="files">
        /// The paths to filter.<br/>
        /// 필터링할 경로 컬렉션입니다.
        /// </param>
        /// <param name="extensionFilter">
        /// The wildcard patterns used for matching.<br/>
        /// 매칭에 사용할 와일드카드 패턴 컬렉션입니다.
        /// </param>
        /// <returns>
        /// A collection containing the paths matched by <paramref name="extensionFilter"/>.<br/>
        /// <paramref name="extensionFilter"/>와 일치하는 경로를 포함하는 컬렉션을 반환합니다.
        /// </returns>
        public static IEnumerable<RuniPath> FilterFiles(IEnumerable<RuniPath> files, WildcardPatterns extensionFilter)
        {
            IEnumerable<string> patterns = extensionFilter.Select(ConvertPatternToRegex);

            // `*` 패턴이 포함되어 있다면 바로 모든 파일 반환
            if (patterns.Contains(".*"))
                return files;

            return files.Where(file => patterns.Any(pattern => Regex.IsMatch(file, pattern, RegexOptions.IgnoreCase))).ToList();

            static string ConvertPatternToRegex(string pattern)
            {
                if (pattern == "*" || pattern == "*.*")
                    return ".*"; // 모든 파일을 허용하는 패턴

                string escaped = Regex.Escape(pattern).Replace(@"\*", ".*"); // '*'를 '.*'로 변환
                return $"^{escaped}$";
            }
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
        public static implicit operator string(RuniPath path) => path.value;

        /// <summary>
        /// Converts a string to a normalized <see cref="RuniPath"/>.<br/>
        /// 문자열을 정규화된 <see cref="RuniPath"/>로 변환합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to convert. It may be <see langword="null"/>.<br/>
        /// 변환할 경로 문자열입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        public static implicit operator RuniPath(string? path) => new RuniPath(path);



        #region + operator
        /// <summary>
        /// Combines two paths.<br/>
        /// 두 경로를 결합합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to combine.<br/>
        /// 결합할 첫 번째 경로입니다.
        /// </param>
        /// <param name="right">
        /// The second path to combine.<br/>
        /// 결합할 두 번째 경로입니다.
        /// </param>
        /// <returns>
        /// The combined path.<br/>
        /// 결합된 경로를 반환합니다.
        /// </returns>
        public static RuniPath operator +(RuniPath left, RuniPath right) => Combine(left, right);

        /// <summary>
        /// Combines a path and a nullable path.<br/>
        /// 경로와 nullable 경로를 결합합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to combine.<br/>
        /// 결합할 첫 번째 경로입니다.
        /// </param>
        /// <param name="right">
        /// The second path to combine. It may be <see langword="null"/>.<br/>
        /// 결합할 두 번째 경로입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <returns>
        /// The combined path.<br/>
        /// 결합된 경로를 반환합니다.
        /// </returns>
        public static RuniPath operator +(RuniPath left, RuniPath? right) => Combine(left, right ?? empty);

        /// <summary>
        /// Combines a nullable path and a path.<br/>
        /// nullable 경로와 경로를 결합합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to combine. It may be <see langword="null"/>.<br/>
        /// 결합할 첫 번째 경로입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="right">
        /// The second path to combine.<br/>
        /// 결합할 두 번째 경로입니다.
        /// </param>
        /// <returns>
        /// The combined path.<br/>
        /// 결합된 경로를 반환합니다.
        /// </returns>
        public static RuniPath operator +(RuniPath? left, RuniPath right) => Combine(left ?? empty, right);

        /// <summary>
        /// Combines two nullable paths.<br/>
        /// 두 nullable 경로를 결합합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to combine. It may be <see langword="null"/>.<br/>
        /// 결합할 첫 번째 경로입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="right">
        /// The second path to combine. It may be <see langword="null"/>.<br/>
        /// 결합할 두 번째 경로입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <returns>
        /// The combined path.<br/>
        /// 결합된 경로를 반환합니다.
        /// </returns>
        public static RuniPath operator +(RuniPath? left, RuniPath? right) => Combine(left ?? empty, right ?? empty);

        /// <summary>
        /// Combines a path and a path string.<br/>
        /// 경로와 경로 문자열을 결합합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to combine.<br/>
        /// 결합할 첫 번째 경로입니다.
        /// </param>
        /// <param name="right">
        /// The second path string to combine. It may be <see langword="null"/>.<br/>
        /// 결합할 두 번째 경로 문자열입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <returns>
        /// The combined path.<br/>
        /// 결합된 경로를 반환합니다.
        /// </returns>
        public static RuniPath operator +(RuniPath left, string? right) => Combine(left, right);

        /// <summary>
        /// Combines a nullable path and a path string.<br/>
        /// nullable 경로와 경로 문자열을 결합합니다.
        /// </summary>
        /// <param name="left">
        /// The first path to combine. It may be <see langword="null"/>.<br/>
        /// 결합할 첫 번째 경로입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <param name="right">
        /// The second path string to combine. It may be <see langword="null"/>.<br/>
        /// 결합할 두 번째 경로 문자열입니다. <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <returns>
        /// The combined path.<br/>
        /// 결합된 경로를 반환합니다.
        /// </returns>
        public static RuniPath operator +(RuniPath? left, string? right) => Combine(left ?? empty, right);
        #endregion



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
        #endregion



        void ISerializationCallbackReceiver.OnBeforeSerialize() => value = value;
        void ISerializationCallbackReceiver.OnAfterDeserialize() => value = value;
    }
}
