#nullable enable
using Newtonsoft.Json;
using RuniOS.Json.Converters.IO;
using System.IO;

namespace RuniOS.IO
{
    /// <summary>
    /// Represents a normalized physical file-system path.<br/>
    /// The stored value is converted to a full path through <see cref="Path.GetFullPath(string)"/>.
    /// <br/><br/>
    /// 정규화된 물리 파일 시스템 경로를 나타냅니다.<br/>
    /// 저장되는 값은 <see cref="Path.GetFullPath(string)"/>를 통해 전체 경로로 변환됩니다.
    /// </summary>
    /// <param name="path">
    /// The path string to normalize.<br/>
    /// 정규화할 경로 문자열입니다.
    /// </param>
    [Serializable]
    [JsonConverter(typeof(PhysicalPathConverter))]
    public struct PhysicalPath(string path) : IEquatable<PhysicalPath>, ISerializationCallbackReceiver
    {
        public static PhysicalPath currentDirectory => new PhysicalPath(string.Empty);

        /// <summary>
        /// Gets the normalized string value of this path.<br/>
        /// 이 경로의 정규화된 문자열 값을 가져옵니다.
        /// </summary>
        public string value
        {
            readonly get => _value ?? NormalizePath(string.Empty);
            set => _value = NormalizePath(value);
        }
        [SerializeField, FieldName("gui.value"), NotNullField, JsonIgnore] string? _value = NormalizePath(path);

        /// <summary>
        /// Gets the length of the normalized path string.<br/>
        /// 정규화된 경로 문자열의 길이를 가져옵니다.
        /// </summary>
        public readonly int length => _value?.Length ?? 0;



        // default로는 여전히 _value가 null이지만, 그래도 일단 value 값 가져올 때 마다 NormalizePath 되는건 안좋으니 매개변수 없는 생성자라도 만들었습니다.
        public PhysicalPath() : this(string.Empty) { }



        /// <summary>
        /// Creates a new <see cref="PhysicalPath"/> from the specified path string.<br/>
        /// 지정된 경로 문자열에서 새 <see cref="PhysicalPath"/>를 생성합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to normalize.<br/>
        /// 정규화할 경로 문자열입니다.
        /// </param>
        /// <returns>
        /// A normalized <see cref="PhysicalPath"/> value.<br/>
        /// 정규화된 <see cref="PhysicalPath"/> 값을 반환합니다.
        /// </returns>
        public static PhysicalPath From(string path) => new PhysicalPath(path);



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
        public readonly RuniPath TrimStartPath(PhysicalPath relativeTo) => new RuniPath(PathUtility.TrimStartPath(value, relativeTo.value));

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
        public readonly bool TryTrimStartPath(PhysicalPath relativeTo, out RuniPath result)
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
        public readonly bool StartsWith(PhysicalPath startPath) => PathUtility.StartsWith(value, startPath.value);



        /// <summary>
        /// Combines this physical path with a logical relative path.<br/>
        /// 이 물리 경로에 논리 상대 경로를 결합합니다.
        /// </summary>
        /// <param name="path">
        /// The logical relative path to append.<br/>
        /// 덧붙일 논리 상대 경로입니다.
        /// </param>
        /// <returns>
        /// The normalized physical path produced by combining this path and <paramref name="path"/>.<br/>
        /// 현재 경로와 <paramref name="path"/>를 결합해 만든 정규화된 물리 경로를 반환합니다.
        /// </returns>
        public readonly PhysicalPath Combine(RuniPath path) => (PhysicalPath)PathUtility.CombineFromNormalizedPath(value, path.value);

        /// <summary>
        /// Combines this physical path with a path string treated as a logical relative path.<br/>
        /// 이 물리 경로에 논리 상대 경로로 취급되는 경로 문자열을 결합합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to normalize with <see cref="RuniPath"/> rules and append.<br/>
        /// <see cref="RuniPath"/> 규칙으로 정규화한 뒤 덧붙일 경로 문자열입니다.
        /// </param>
        /// <returns>
        /// The normalized physical path produced by combining this path and <paramref name="path"/>.<br/>
        /// 현재 경로와 <paramref name="path"/>를 결합해 만든 정규화된 물리 경로를 반환합니다.
        /// </returns>
        public readonly PhysicalPath Combine(string path) => (PhysicalPath)PathUtility.CombineFromNormalizedPath(value, RuniPath.NormalizePath(path));



        /// <summary>
        /// Normalizes a path string into the internal physical path format.<br/>
        /// The path is first resolved with <see cref="Path.GetFullPath(string)"/>, then trailing separators outside the root are removed.
        /// <br/><br/>
        /// 경로 문자열을 내부 물리 경로 형식으로 정규화합니다.<br/>
        /// 먼저 <see cref="Path.GetFullPath(string)"/>로 경로를 해석한 뒤, 루트 밖의 끝 구분자를 제거합니다..
        /// </summary>
        /// <param name="path">
        /// The path string to normalize.<br/>
        /// 정규화할 경로 문자열입니다.
        /// </param>
        /// <returns>
        /// The normalized physical path string.<br/>
        /// 정규화된 물리 경로 문자열을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown by <see cref="Path.GetFullPath(string)"/> when <paramref name="path"/> is invalid.<br/>
        /// <paramref name="path"/>가 잘못된 경우 <see cref="Path.GetFullPath(string)"/>에서 발생합니다.
        /// </exception>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                path = Directory.GetCurrentDirectory();

            string fullPath = Path.GetFullPath(path);
            int rootLength = Path.GetPathRoot(fullPath.AsSpan()).Length;

            int trimmedLength = fullPath.Length;
            while (trimmedLength > rootLength && (fullPath[trimmedLength - 1] == Path.DirectorySeparatorChar || fullPath[trimmedLength - 1] == Path.AltDirectorySeparatorChar))
                trimmedLength--;

            bool needsCopy = trimmedLength != fullPath.Length || fullPath.Contains(Path.AltDirectorySeparatorChar);
            if (!needsCopy)
                return fullPath;

            return string.Create(trimmedLength, fullPath, static (span, state) =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    char c = state[i];
                    if (c == Path.AltDirectorySeparatorChar)
                        span[i] = Path.DirectorySeparatorChar;
                    else
                        span[i] = c;
                }
            });
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
        /// <see langword="true"/> if <paramref name="obj"/> is an equal <see cref="PhysicalPath"/>; otherwise, <see langword="false"/>.<br/>
        /// <paramref name="obj"/>가 같은 <see cref="PhysicalPath"/>이면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public override readonly bool Equals(object? obj) => obj is PhysicalPath path && Equals(path);

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
        public readonly bool Equals(PhysicalPath other) => value == other.value;
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



        #region operators
        /// <summary>
        /// Converts a <see cref="PhysicalPath"/> to its normalized string value.<br/>
        /// <see cref="PhysicalPath"/>를 정규화된 문자열 값으로 변환합니다.
        /// </summary>
        /// <param name="path">
        /// The path to convert.<br/>
        /// 변환할 경로입니다.
        /// </param>
        public static explicit operator string(PhysicalPath path) => path.value;

        /// <summary>
        /// Converts a string to a normalized <see cref="PhysicalPath"/>.<br/>
        /// 문자열을 정규화된 <see cref="PhysicalPath"/>로 변환합니다.
        /// </summary>
        /// <param name="path">
        /// The path string to convert.<br/>
        /// 변환할 경로 문자열입니다.
        /// </param>
        public static explicit operator PhysicalPath(string path) => new PhysicalPath(path);



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
        public static bool operator ==(PhysicalPath left, PhysicalPath right) => left.Equals(right);

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
        public static bool operator ==(PhysicalPath left, string right) => left.value.Equals(right);

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
        public static bool operator ==(string left, PhysicalPath right) => left.Equals(right.value);

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
        public static bool operator !=(PhysicalPath left, PhysicalPath right) => !(left == right);

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
        public static bool operator !=(PhysicalPath left, string right) => !(left == right);

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
        public static bool operator !=(string left, PhysicalPath right) => !(left == right);
        #endregion



        void ISerializationCallbackReceiver.OnBeforeSerialize() => value = _value ?? string.Empty;
        void ISerializationCallbackReceiver.OnAfterDeserialize() => value = _value ?? string.Empty;
    }
}
