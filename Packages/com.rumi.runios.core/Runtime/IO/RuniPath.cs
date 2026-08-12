#nullable enable
using Newtonsoft.Json;
using RuniOS.Json.Converters.IO;
using RuniOS.Spans;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace RuniOS.IO
{
    /// <summary>
    /// Represents a normalized logical path used by the framework.<br/>
    /// Normalization trims leading and trailing <see cref="directorySeparatorChar"/> values and collapses repeated <see cref="directorySeparatorChar"/> separators.
    /// <br/><br/>
    /// 프레임워크에서 사용하는 정규화된 논리 경로를 나타냅니다.<br/>
    /// 정규화 과정에서 시작과 끝의 <see cref="directorySeparatorChar"/> 값을 제거하고 반복된 <see cref="directorySeparatorChar"/> 구분자를 합칩니다.
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
        [SerializeField, FieldName("runios-editor:gui.value"), NotNullField, Delayed, JsonIgnore] string? _value;

        /// <summary>
        /// Gets the length of the normalized path string.<br/>
        /// 정규화된 경로 문자열의 길이를 가져옵니다.
        /// </summary>
        public readonly int length => _value?.Length ?? 0;



        public RuniPath(string path) => _value = NormalizePath(path);
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
        public readonly string GetFileName() => RuniPathUtility.GetFileName(value).ToString();

        /// <summary>
        /// Gets the last path segment without its extension.<br/>
        /// 마지막 경로 세그먼트에서 확장자를 제외한 값을 가져옵니다.
        /// </summary>
        /// <returns>
        /// The last path segment without its extension, or the full segment when no extension exists.<br/>
        /// 확장자를 제외한 마지막 경로 세그먼트를 반환하며, 확장자가 없으면 세그먼트 전체를 반환합니다.
        /// </returns>
        public readonly string GetFileNameWithoutExtension() => RuniPathUtility.GetFileNameWithoutExtension(value).ToString();

        /// <summary>
        /// Gets a path with the extension removed from the last segment.<br/>
        /// 마지막 세그먼트의 확장자를 제거한 경로를 가져옵니다.
        /// </summary>
        /// <returns>
        /// A path without the last segment extension, or this path when no extension exists.<br/>
        /// 마지막 세그먼트의 확장자가 제거된 경로를 반환하며, 확장자가 없으면 현재 경로를 반환합니다.
        /// </returns>
        public readonly RuniPath GetPathWithoutExtension() => new RuniPath { _value = RuniPathUtility.GetPathWithoutExtension(value).ToString() };

        /// <summary>
        /// Gets the path that contains every segment except the last one.<br/>
        /// 마지막 세그먼트를 제외한 나머지 경로를 가져옵니다.
        /// </summary>
        /// <returns>
        /// The parent path, or <see cref="empty"/> when this path has no parent segment.<br/>
        /// 상위 경로를 반환하며, 상위 세그먼트가 없으면 <see cref="empty"/>를 반환합니다.
        /// </returns>
        public readonly RuniPath GetParentPath() => new RuniPath { _value = RuniPathUtility.GetParentPath(value).ToString() };



        /// <summary>
        /// Removes the specified prefix path when this path is under it.<br/>
        /// 이 경로가 지정된 접두사 경로 아래에 있으면 해당 접두사를 제거합니다.
        /// </summary>
        /// <param name="relativeTo">
        /// The prefix path to remove.<br/>
        /// 제거할 접두사 경로입니다.
        /// </param>
        /// <returns>
        /// The path with the prefix removed when the prefix matches; otherwise, this path.<br/>
        /// 접두사가 일치하면 제거된 경로를 반환하고, 그렇지 않으면 현재 경로를 반환합니다.
        /// </returns>
        public readonly RuniPath GetRelativePath(RuniPath relativeTo) => new RuniPath { _value = RuniPathUtility.GetRelativePath(value, relativeTo.value).ToString() };

        /// <summary>
        /// Attempts to remove the specified prefix path from this path.<br/>
        /// 이 경로에서 지정된 접두사 경로 제거를 시도합니다.
        /// </summary>
        /// <param name="relativeTo">
        /// The prefix path to remove.<br/>
        /// 제거할 접두사 경로입니다.
        /// </param>
        /// <param name="result">
        /// When this method returns <see langword="true"/>, contains the path with the prefix removed.<br/>
        /// 이 메서드가 <see langword="true"/>를 반환하면 접두사가 제거된 경로를 포함합니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the prefix matches; otherwise, <see langword="false"/>.<br/>
        /// 접두사가 일치하면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public readonly bool TryGetRelativePath(RuniPath relativeTo, out RuniPath result)
        {
            bool success = RuniPathUtility.TryGetRelativePath(value, relativeTo.value, out ReadOnlySpan<char> span);
            result = new RuniPath { _value = span.ToString() };
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
        public readonly bool StartsWith(RuniPath startPath) => RuniPathUtility.StartsWith(value, startPath.value);



        public string[] GetSegments() => value.Split(directorySeparatorChar);
        public ReadOnlySpanSingleSplitter<char> GetSegmentsSpan() => value.AsSpan().Split(directorySeparatorChar);



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
        public readonly RuniPath AddExtension(FileExtension ext) => new RuniPath { _value = value + ext };

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
        public readonly RuniPath AddExtension(string ext) => new RuniPath { _value = value + (FileExtension)ext };

        public readonly RuniPath SetExtension(FileExtension ext) => GetPathWithoutExtension().AddExtension(ext);
        public readonly RuniPath SetExtension(string ext) => GetPathWithoutExtension().AddExtension(ext);



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
        public readonly RuniPath Combine(RuniPath path)
        {
            if (length == 0 && path.length == 0)
                return empty;
            else if (length == 0)
                return path;
            else if (path.length == 0)
                return this;

            return new RuniPath
            {
                _value = string.Create(length + 1 + path.length, (left: this, right: path), static (span, state) =>
                {
                    int index = 0;
                    for (int i = 0; i < state.left.length; i++)
                    {
                        span[index] = state.left.value[i];
                        index++;
                    }

                    span[index] = directorySeparatorChar;
                    index++;

                    for (int i = 0; i < state.right.length; i++)
                    {
                        span[index] = state.right.value[i];
                        index++;
                    }
                })
            };
        }

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
        public readonly RuniPath Combine(string path) => Combine(new RuniPath(path));



        /// <summary>
        /// Normalizes a path string into the <see cref="RuniPath"/> text format.<br/>
        /// The result trims leading and trailing <see cref="directorySeparatorChar"/> values and collapses repeated <see cref="directorySeparatorChar"/> separators without interpreting dot segments.
        /// <br/><br/>
        /// 경로 문자열을 <see cref="RuniPath"/> 텍스트 형식으로 정규화합니다.<br/>
        /// 결과는 시작과 끝의 <see cref="directorySeparatorChar"/> 값을 제거하고 반복된 <see cref="directorySeparatorChar"/> 구분자를 합치며, 점 세그먼트를 해석하지 않습니다.
        /// </summary>
        /// <param name="path">
        /// The path string to normalize.<br/>
        /// 정규화할 경로 문자열입니다.
        /// </param>
        /// <returns>
        /// The normalized path string, or <see cref="string.Empty"/> when <paramref name="path"/> has no usable segment.<br/>
        /// 정규화된 경로 문자열을 반환하며, <paramref name="path"/>에 사용할 수 있는 세그먼트가 없으면 <see cref="string.Empty"/>를 반환합니다.
        /// </returns>
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
                foreach (var item in path.AsSpan().Trim(directorySeparatorChar).Split(directorySeparatorChar))
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

            path = path.Trim(directorySeparatorChar);
            if (path.IsEmpty)
                return 0;

            int length = 0;
            foreach (var item in path.Split(directorySeparatorChar))
            {
                if (item.IsEmpty)
                    continue;

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



        #region ==
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
        #endregion

        #region !=
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



        #region /
        public static RuniPath operator /(RuniPath left, RuniPath right) => left.Combine(right);
        public static RuniPath operator /(RuniPath left, string right) => left.Combine(right);
        #endregion
        #endregion



        void ISerializationCallbackReceiver.OnBeforeSerialize() => value = value;
        void ISerializationCallbackReceiver.OnAfterDeserialize() => value = value;
    }
}
