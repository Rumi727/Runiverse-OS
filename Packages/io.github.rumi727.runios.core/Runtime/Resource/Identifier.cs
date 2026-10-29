#nullable enable
using Newtonsoft.Json;
using RuniOS.IO;
using RuniOS.Json.Converters.Resource;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace RuniOS.Resource
{
    /// <summary>
    /// 네임스페이스와 경로로 구성된 리소스에 대한 고유 식별자를 나타냅니다.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(IdentifierConverter))]
    public struct Identifier : IEquatable<Identifier>, ISerializationCallbackReceiver
    {
        public static readonly Identifier empty = new Identifier();

        /// <summary>
        /// Gets the system fallback namespace used by serializers and other code paths without an explicit owner namespace.<br/>
        /// 직렬화기 및 명시적인 소유 네임스페이스가 없는 코드 경로에서 사용하는 시스템 폴백 네임스페이스를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// Prefer an explicit namespace, such as <c>"namespace:path"</c> or a constructor, for runtime and persistent IDs.<br/>
        /// When a string without a namespace is passed to <see cref="Parse(string, string?)"/> with a <see langword="null"/> or empty default namespace, or to the implicit string conversion, this namespace is used.
        /// <br/><br/>
        /// 런타임 및 영속 ID에는 <c>"namespace:path"</c> 또는 생성자처럼 명시적인 네임스페이스를 우선 사용하세요.<br/>
        /// 네임스페이스가 없는 문자열을 <see langword="null"/> 또는 빈 기본 네임스페이스로 <see cref="Parse(string, string?)"/>에 전달하거나 문자열 암시 변환에 전달하면 이 네임스페이스를 사용합니다.
        /// </remarks>
        public const string defaultNamespace = "runios";

        /// <summary>
        /// 네임스페이스와 경로를 구분하는 데 사용되는 문자입니다.
        /// </summary>
        public const char separator = ':';

        /// <summary>
        /// Initializes a new <see cref="Identifier"/> with the specified namespace and path.<br/>
        /// 지정된 네임스페이스와 경로로 새 <see cref="Identifier"/>를 초기화합니다.
        /// </summary>
        /// <param name="nameSpace">
        /// The namespace component. Prefer passing a valid, explicit namespace.<br/>
        /// 네임스페이스 구성 요소입니다. 유효한 명시적 네임스페이스를 전달하는 방식을 권장합니다.
        /// </param>
        /// <param name="path">
        /// The path component of the identifier.<br/>
        /// 식별자의 경로 구성 요소입니다.
        /// </param>
        /// <remarks>
        /// Use this constructor when the namespace and path are already separate values.<br/>
        /// For literals, the implicit conversion from <c>"namespace:path"</c> is the shorter equivalent.
        /// <br/><br/>
        /// 네임스페이스와 경로가 이미 분리된 값일 때 이 생성자를 사용하세요.<br/>
        /// 리터럴에는 <c>"namespace:path"</c> 암시 변환이 더 짧은 동등한 방법입니다.
        /// </remarks>
        /// <exception cref="InvalidIdentifierException">
        /// Thrown when <paramref name="nameSpace"/> or <paramref name="path"/> is invalid.<br/>
        /// <paramref name="nameSpace"/> 또는 <paramref name="path"/>가 유효하지 않으면 발생합니다.
        /// </exception>
        public Identifier(string nameSpace, string path)
        {
            if (!IsNamespaceValid(nameSpace))
                throw new InvalidIdentifierException(GetInvalidNamespaceMessage(nameSpace));

            RuniPath runiPath = (RuniPath)path;
            if (!IsPathValid(runiPath))
                throw new InvalidIdentifierException(GetInvalidPathMessage(runiPath));

            _nameSpace = nameSpace;
            _path = runiPath;
        }

        /// <summary>
        /// Initializes a new <see cref="Identifier"/> with the specified namespace and path.<br/>
        /// 지정된 네임스페이스와 경로로 새 <see cref="Identifier"/>를 초기화합니다.
        /// </summary>
        /// <param name="nameSpace">
        /// The namespace component. Prefer passing a valid, explicit namespace.<br/>
        /// 네임스페이스 구성 요소입니다. 유효한 명시적 네임스페이스를 전달하는 방식을 권장합니다.
        /// </param>
        /// <param name="path">
        /// The path component of the identifier.<br/>
        /// 식별자의 경로 구성 요소입니다.
        /// </param>
        /// <remarks>
        /// Use this constructor when the namespace and path are already separate values.<br/>
        /// For literals, the implicit conversion from <c>"namespace:path"</c> is the shorter equivalent.
        /// <br/><br/>
        /// 네임스페이스와 경로가 이미 분리된 값일 때 이 생성자를 사용하세요.<br/>
        /// 리터럴에는 <c>"namespace:path"</c> 암시 변환이 더 짧은 동등한 방법입니다.
        /// </remarks>
        /// <exception cref="InvalidIdentifierException">
        /// Thrown when <paramref name="nameSpace"/> or <paramref name="path"/> is invalid.<br/>
        /// <paramref name="nameSpace"/> 또는 <paramref name="path"/>가 유효하지 않으면 발생합니다.
        /// </exception>
        public Identifier(string nameSpace, RuniPath path)
        {
            if (!IsNamespaceValid(nameSpace))
                throw new InvalidIdentifierException(GetInvalidNamespaceMessage(nameSpace));

            if (!IsPathValid(path))
                throw new InvalidIdentifierException(GetInvalidPathMessage(path));

            _nameSpace = nameSpace;
            _path = path;
        }



        /// <summary>
        /// 식별자의 네임스페이스 구성 요소를 가져오거나 설정합니다.
        /// </summary>
        [AllowNull]
        public string nameSpace
        {
            readonly get => string.IsNullOrEmpty(_nameSpace) ? defaultNamespace : _nameSpace;
            set
            {
                if (string.IsNullOrEmpty(value))
                    _nameSpace = defaultNamespace;
                else if (IsNamespaceValid(value))
                    _nameSpace = value;
                else
                    throw new InvalidIdentifierException(GetInvalidNamespaceMessage(value));
            }
        }
        [SerializeField, FieldName("runios-editor:gui.namespace"), NotNullField, JsonIgnore] string? _nameSpace;

        /// <summary>
        /// 식별자의 경로 구성 요소를 가져오거나 설정합니다.
        /// </summary>
        public RuniPath path
        {
            readonly get => _path;
            set
            {
                if (IsPathValid(value))
                    _path = value;
                else
                    throw new InvalidIdentifierException(GetInvalidPathMessage(value));
            }
        }
        [SerializeField, FieldName("runios-editor:gui.path"), JsonIgnore] RuniPath _path;



        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (nameSpace == defaultNamespace)
                nameSpace = null;
            else if (!IsNamespaceValid(nameSpace))
                nameSpace = defaultNamespace;

            if (!IsPathValid(path))
                path = RuniPath.empty;
        }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (nameSpace == defaultNamespace)
                nameSpace = null;
            else if (!IsNamespaceValid(nameSpace))
                nameSpace = defaultNamespace;

            if (!IsPathValid(path))
                path = RuniPath.empty;
        }



        /// <summary>
        /// 이 식별자의 문자열 표현을 반환합니다.
        /// </summary>
        /// <returns>"namespace:path" 형식의 문자열입니다.</returns>
        public override readonly string ToString() => nameSpace + separator + path.ToString();



        public static bool operator ==(Identifier lhs, Identifier rhs) => lhs.nameSpace == rhs.nameSpace && lhs.path == rhs.path;
        public static bool operator !=(Identifier lhs, Identifier rhs) => !(lhs == rhs);



        /// <summary>
        /// 이 <see cref="Identifier"/> 인스턴스와 다른 지정된 <see cref="Identifier"/> 인스턴스의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 <see cref="Identifier"/>입니다.</param>
        /// <returns>지정된 <see cref="Identifier"/>가 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public readonly bool Equals(Identifier other) => this == other;

        /// <summary>
        /// 이 <see cref="Identifier"/> 인스턴스와 지정된 <see cref="object"/>의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="obj">현재 인스턴스와 비교할 <see cref="object"/>입니다.</param>
        /// <returns>지정된 <see cref="object"/>가 <see cref="Identifier"/>이고 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public override readonly bool Equals(object? obj)
        {
            if (obj is Identifier otherIdentifier)
                return Equals(otherIdentifier);

            return false;
        }

        /// <summary>
        /// 이 <see cref="Identifier"/> 인스턴스의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>32비트 부호 있는 정수 해시 코드입니다.</returns>
        public override readonly int GetHashCode() => nameSpace.GetHashCode() * path.GetHashCode();



        public void Deconstruct(out string nameSpace, out RuniPath path)
        {
            nameSpace = this.nameSpace;
            path = this.path;
        }



        public static implicit operator string(Identifier identifier) => identifier.ToString();

        /// <summary>
        /// Converts a string in <c>namespace:path</c> or <c>path</c> form into an <see cref="Identifier"/>.<br/>
        /// <c>namespace:path</c> 또는 <c>path</c> 형식의 문자열을 <see cref="Identifier"/>로 변환합니다.
        /// </summary>
        /// <param name="identifier">
        /// The identifier string to convert.<br/>
        /// 변환할 식별자 문자열입니다.
        /// </param>
        /// <remarks>
        /// The <c>namespace:path</c> form costs about the same as <see cref="Identifier(string, string)"/> and is the preferred way to write identifier literals.<br/>
        /// The <c>path</c> form always uses <see cref="defaultNamespace"/>.
        /// <br/><br/>
        /// <c>namespace:path</c> 형식은 <see cref="Identifier(string, string)"/>와 비슷한 비용이며, 식별자 리터럴을 작성할 때 권장되는 방법입니다.<br/>
        /// <c>path</c> 형식은 항상 <see cref="defaultNamespace"/>를 사용합니다.
        /// </remarks>
        public static implicit operator Identifier(string identifier) => Parse(identifier);



        /// <summary>
        /// Parses a string in <c>namespace:path</c> or <c>path</c> form into an <see cref="Identifier"/>.<br/>
        /// <c>namespace:path</c> 또는 <c>path</c> 형식의 문자열을 <see cref="Identifier"/>로 구문 분석합니다.
        /// </summary>
        /// <param name="identifier">
        /// The identifier string to parse.<br/>
        /// 구문 분석할 식별자 문자열입니다.
        /// </param>
        /// <param name="defaultNamespace">
        /// The namespace used when <paramref name="identifier"/> has no namespace. If this is <see langword="null"/> or empty, <see cref="Identifier.defaultNamespace"/> is used.<br/>
        /// <paramref name="identifier"/>에 네임스페이스가 없을 때 사용할 네임스페이스입니다. <see langword="null"/>이거나 비어 있으면 <see cref="Identifier.defaultNamespace"/>를 사용합니다.
        /// </param>
        /// <returns>
        /// The parsed <see cref="Identifier"/>.<br/>
        /// 구문 분석된 <see cref="Identifier"/>입니다.
        /// </returns>
        /// <remarks>
        /// The <c>namespace:path</c> form costs about the same as <see cref="Identifier(string, string)"/>.<br/>
        /// <c>namespace:path</c> 형식은 <see cref="Identifier(string, string)"/>와 비슷한 비용입니다.
        /// </remarks>
        /// <exception cref="InvalidIdentifierException">
        /// Thrown when <paramref name="identifier"/> has an invalid format or contains invalid namespace or path characters.<br/>
        /// <paramref name="identifier"/>의 형식이 유효하지 않거나 네임스페이스 또는 경로에 유효하지 않은 문자가 있으면 발생합니다.
        /// </exception>
        // ReSharper restore Unity.ExpensiveCode
        public static Identifier Parse(string identifier, string? defaultNamespace = null)
        {
            int separatorIndex = identifier.AsSpan().IndexOf(separator);

            // 구분자가 없는 경우, 전체 문자열을 경로로 간주하고 네임스페이스는 기본값으로 설정합니다.
            if (separatorIndex < 0)
                return new Identifier(string.IsNullOrEmpty(defaultNamespace) ? Identifier.defaultNamespace : defaultNamespace, identifier);

            if (identifier.AsSpan(separatorIndex + 1).IndexOf(separator) >= 0)
                throw new InvalidIdentifierException($"Invalid identifier format: '{identifier}'. Contains too many '{separator}' separators.");

            return new Identifier(identifier.Substring(0, separatorIndex), identifier.Substring(separatorIndex + 1));
        }

        /// <summary>
        /// 네임스페이스의 유효성을 검사합니다.
        /// 허용되는 문자: 'a-z', '0-9', '.', '-', '_'
        /// </summary>
        public static bool IsNamespaceValid([NotNullWhen(false)] string? nameSpace)
        {
            if (string.IsNullOrEmpty(nameSpace))
                return true; // 빈 문자열 또는 null은 유효한 네임스페이스로 간주 (기본값을 사용하거나 생략 가능)

            return nameSpace.All(static item =>
                (item >= 'a' && item <= 'z') || (item >= '0' && item <= '9') || item == '.' || item == '-' || item == '_');
        }

        /// <summary>
        /// 경로의 유효성을 검사합니다.
        /// 허용되는 문자: 'a-z', '0-9', '.', '/', '-', '_'
        /// </summary>
        public static bool IsPathValid(RuniPath path)
        {
            if (path.IsEmpty())
                return true; // 비어있는 경로는 유효한 경로로 간주

            return path.value.All(static item =>
                (item >= 'a' && item <= 'z') || (item >= '0' && item <= '9') || item == '.' || item == '/' || item == '-' || item == '_');
        }



        public static string GetInvalidNamespaceMessage(string? nameSpace) => $"Invalid namespace: '{nameSpace}'. Allowed characters are 'a-z', '0-9', '.', '-', and '_'.";

        public static string GetInvalidPathMessage(RuniPath path) => $"Invalid path: '{path}'. Allowed characters are 'a-z', '0-9', '.', '/', '-', and '_'.";
    }
}
