#nullable enable
using Newtonsoft.Json;
using RuniOS.Spans;
using RuniOS.IO;
using RuniOS.Json.Converters.Resource;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        /// Prefer constructors with an explicit namespace for runtime and persistent IDs.<br/>
        /// <see cref="Parse(string, string?)"/> with a <see langword="null"/> or empty default namespace and the implicit string conversion are convenience APIs that use the calling assembly default namespace.
        /// <br/><br/>
        /// 런타임 및 영속 ID에는 명시적인 네임스페이스를 받는 생성자를 우선 사용하세요.<br/>
        /// <see cref="Parse(string, string?)"/>에 <see langword="null"/> 또는 빈 기본 네임스페이스를 전달한 경우와 문자열 암시 변환은 호출 어셈블리의 기본 네임스페이스를 사용하는 편의 API입니다.
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
        /// This constructor is the preferred fast path when the namespace is already known.<br/>
        /// <see cref="Parse(string, string?)"/> and the implicit string conversion are heavier convenience APIs intended for UI or authoring code.
        /// <br/><br/>
        /// 네임스페이스를 이미 알고 있을 때는 이 생성자가 권장되는 빠른 경로입니다.<br/>
        /// <see cref="Parse(string, string?)"/>와 문자열 암시 변환은 UI 또는 저작 코드용의 더 무거운 편의 API입니다.
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
        /// This constructor is the preferred fast path when the namespace is already known.<br/>
        /// <see cref="Parse(string, string?)"/> and the implicit string conversion are heavier convenience APIs intended for UI or authoring code.
        /// <br/><br/>
        /// 네임스페이스를 이미 알고 있을 때는 이 생성자가 권장되는 빠른 경로입니다.<br/>
        /// <see cref="Parse(string, string?)"/>와 문자열 암시 변환은 UI 또는 저작 코드용의 더 무거운 편의 API입니다.
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
        [SerializeField, FieldName("gui.namespace"), NotNullField, JsonIgnore] string? _nameSpace;

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
        [SerializeField, FieldName("gui.path"), JsonIgnore] RuniPath _path;



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
        /// This is a convenience API. It performs parsing and may inspect the calling assembly when <paramref name="identifier"/> has no namespace.<br/>
        /// Prefer <see cref="Identifier(string, RuniPath)"/> or <see cref="Identifier(string, string)"/> when performance, persistence, or explicit ownership matters.
        /// <br/><br/>
        /// 이 API는 편의 API입니다. 파싱을 수행하며, <paramref name="identifier"/>에 네임스페이스가 없으면 호출 어셈블리를 확인할 수 있습니다.<br/>
        /// 성능, 영속성, 명시적 소유권이 중요하면 <see cref="Identifier(string, RuniPath)"/> 또는 <see cref="Identifier(string, string)"/>를 우선 사용하세요.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static implicit operator Identifier(string identifier) => Parse(identifier, GetDefaultNamespace(Assembly.GetCallingAssembly()));



        /// <summary>
        /// Gets the default identifier namespace of the calling assembly.<br/>
        /// 호출 어셈블리의 기본 식별자 네임스페이스를 가져옵니다.
        /// </summary>
        /// <returns>
        /// The namespace from <see cref="DefaultIdentifierNamespaceAttribute"/> on the calling assembly, or <see cref="defaultNamespace"/> if none is set.<br/>
        /// 호출 어셈블리의 <see cref="DefaultIdentifierNamespaceAttribute"/> 네임스페이스입니다. 지정되지 않았으면 <see cref="defaultNamespace"/>입니다.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentNamespace() => GetDefaultNamespace(Assembly.GetCallingAssembly());

        /// <summary>
        /// Gets the default identifier namespace configured for <paramref name="assembly"/>.<br/>
        /// <paramref name="assembly"/>에 설정된 기본 식별자 네임스페이스를 가져옵니다.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose default namespace should be resolved.<br/>
        /// 기본 네임스페이스를 확인할 어셈블리입니다.
        /// </param>
        /// <returns>
        /// The namespace from <see cref="DefaultIdentifierNamespaceAttribute"/>, or <see cref="defaultNamespace"/> if the attribute is not present.<br/>
        /// <see cref="DefaultIdentifierNamespaceAttribute"/>의 네임스페이스입니다. 특성이 없으면 <see cref="defaultNamespace"/>입니다.
        /// </returns>
        public static string GetDefaultNamespace(Assembly assembly) => assembly.GetCustomAttribute<DefaultIdentifierNamespaceAttribute>()?.nameSpace ?? defaultNamespace;



        /// <summary>
        /// Parses a string in <c>namespace:path</c> or <c>path</c> form into an <see cref="Identifier"/>.<br/>
        /// <c>namespace:path</c> 또는 <c>path</c> 형식의 문자열을 <see cref="Identifier"/>로 구문 분석합니다.
        /// </summary>
        /// <param name="identifier">
        /// The identifier string to parse.<br/>
        /// 구문 분석할 식별자 문자열입니다.
        /// </param>
        /// <param name="defaultNamespace">
        /// The namespace used when <paramref name="identifier"/> has no namespace. If this is <see langword="null"/> or empty, the default namespace of the calling assembly is used.<br/>
        /// <paramref name="identifier"/>에 네임스페이스가 없을 때 사용할 네임스페이스입니다. <see langword="null"/>이거나 비어 있으면 호출 어셈블리의 기본 네임스페이스를 사용합니다.
        /// </param>
        /// <returns>
        /// The parsed <see cref="Identifier"/>.<br/>
        /// 구문 분석된 <see cref="Identifier"/>입니다.
        /// </returns>
        /// <remarks>
        /// This is a convenience API intended for UI and authoring code. It is heavier than the constructors because it parses text and can inspect the calling assembly.<br/>
        /// Prefer <see cref="Identifier(string, RuniPath)"/> or <see cref="Identifier(string, string)"/> when the namespace and path are already known.
        /// <br/><br/>
        /// 이 API는 UI 및 저작 코드용 편의 API입니다. 텍스트를 파싱하고 호출 어셈블리를 확인할 수 있으므로 생성자보다 무겁습니다.<br/>
        /// 네임스페이스와 경로를 이미 알고 있으면 <see cref="Identifier(string, RuniPath)"/> 또는 <see cref="Identifier(string, string)"/>를 우선 사용하세요.
        /// </remarks>
        /// <exception cref="InvalidIdentifierException">
        /// Thrown when <paramref name="identifier"/> has an invalid format or contains invalid namespace or path characters.<br/>
        /// <paramref name="identifier"/>의 형식이 유효하지 않거나 네임스페이스 또는 경로에 유효하지 않은 문자가 있으면 발생합니다.
        /// </exception>
        // ReSharper restore Unity.ExpensiveCode
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Identifier Parse(string identifier, string? defaultNamespace = null)
        {
            // 식별자 문자열을 구분자 (':')를 기준으로 분리합니다.
            var splitSpan = identifier.AsSpan().Split(separator);

            string nameSpace = string.Empty;
            string path = string.Empty;

            // 분리된 각 부분을 순회하여 네임스페이스와 경로를 할당합니다.
            int splitCount = 0;
            foreach (var item in splitSpan)
            {
                if (splitCount == 0)
                    nameSpace = new string(item); // 첫 번째 부분은 네임스페이스로 간주합니다.
                else if (splitCount == 1)
                    path = new string(item); // 두 번째 부분은 경로로 간주합니다.

                splitCount++;
            }

            // 분리된 부분의 개수에 따라 유효성을 검사하고 값을 조정합니다.
            if (splitCount <= 0)
                return empty;
            else if (splitCount == 1)
            {
                // 구분자가 없는 경우, 전체 문자열을 경로로 간주하고 네임스페이스는 기본값으로 설정합니다.
                path = nameSpace;
                nameSpace = string.IsNullOrEmpty(defaultNamespace) ? GetDefaultNamespace(Assembly.GetCallingAssembly()) : defaultNamespace;
            }
            else if (splitCount > 2)
                throw new InvalidIdentifierException($"Invalid identifier format: '{identifier}'. Contains too many '{separator}' separators."); // 구분자가 너무 많은 경우 예외를 발생시킵니다.

            // 구문 분석된 네임스페이스와 경로로 새 Identifier 인스턴스를 생성하여 반환합니다.
            return new Identifier(nameSpace, path);
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
