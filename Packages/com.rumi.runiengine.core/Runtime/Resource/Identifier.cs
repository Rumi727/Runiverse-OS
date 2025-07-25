#nullable enable
using Newtonsoft.Json;
using RuniEngine.IO;
using RuniEngine.Spans;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace RuniEngine.Resource
{
    /// <summary>
    /// 네임스페이스와 경로로 구성된 리소스에 대한 고유 식별자를 나타냅니다.
    /// </summary>
    [Serializable]
    public struct Identifier : IEquatable<Identifier>
    {
        /// <summary>
        /// 네임스페이스가 지정되지 않았을 때 사용되는 기본 네임스페이스입니다.
        /// </summary>
        public const string defaultNamespace = "ros";

        /// <summary>
        /// 네임스페이스와 경로를 구분하는 데 사용되는 문자입니다.
        /// </summary>
        public const char separator = ':';

        /// <summary>
        /// 지정된 경로와 기본 네임스페이스로 <see cref="Identifier"/> 구조체의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="path">식별자의 경로 구성 요소입니다.</param>
        /// <exception cref="InvalidIdentifierException">제공된 경로가 유효하지 않은 경우 발생합니다.</exception>
        public Identifier(string path)
        {
            if (!IsPathValid(path))
                throw new InvalidIdentifierException($"Invalid path: '{path}'. Allowed characters are 'a-z', '0-9', '.', '/', '-', and '_'.");

            _nameSpace = defaultNamespace;
            _path = path;
        }

        /// <summary>
        /// 지정된 네임스페이스와 경로로 <see cref="Identifier"/> 구조체의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="nameSpace">식별자의 네임스페이스 구성 요소입니다. null 또는 비어 있는 경우, <see cref="defaultNamespace"/>가 사용됩니다.</param>
        /// <param name="path">식별자의 경로 구성 요소입니다.</param>
        /// <exception cref="InvalidIdentifierException">제공된 네임스페이스 또는 경로가 유효하지 않은 경우 발생합니다.</exception>
        public Identifier(string nameSpace, string path)
        {
            if (!IsNamespaceValid(nameSpace))
                throw new InvalidIdentifierException($"Invalid namespace: '{nameSpace}'. Allowed characters are 'a-z', '0-9', '.', '-', and '_'.");

            if (!IsPathValid(path))
                throw new InvalidIdentifierException($"Invalid path: '{path}'. Allowed characters are 'a-z', '0-9', '.', '/', '-', and '_'.");

            _nameSpace = string.IsNullOrEmpty(nameSpace) ? defaultNamespace : nameSpace;
            _path = path;
        }

        /// <summary>
        /// 식별자의 네임스페이스 구성 요소를 가져오거나 설정합니다.
        /// </summary>
        [AllowNull]
        public string nameSpace
        {
            readonly get => _nameSpace ?? string.Empty;
            set => _nameSpace = value;
        }
        [SerializeField, FieldName("gui.namespace"), NotNullField, JsonIgnore] string? _nameSpace;

        /// <summary>
        /// 식별자의 경로 구성 요소를 가져오거나 설정합니다.
        /// </summary>
        public FilePath path
        {
            readonly get => _path;
            set => _path = value;
        }
        [SerializeField, FieldName("gui.path"), JsonIgnore] FilePath _path;



        /// <summary>
        /// 이 <see cref="Identifier"/> 인스턴스와 다른 지정된 <see cref="Identifier"/> 인스턴스의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 <see cref="Identifier"/>입니다.</param>
        /// <returns>지정된 <see cref="Identifier"/>가 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public readonly bool Equals(Identifier other) => throw new NotImplementedException();

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

        /// <summary>
        /// 이 식별자의 문자열 표현을 반환합니다.
        /// </summary>
        /// <returns>네임스페이스가 비어 있거나 기본값인 경우 "path" 형식의 문자열이고, 그렇지 않으면 "namespace:path" 형식의 문자열입니다.</returns>
        public override readonly string ToString()
        {
            if (string.IsNullOrEmpty(nameSpace)) // 기본 네임스페이스도 짧은 문자열을 위해 고려
                return _path.ToString();
            else
                return _nameSpace + separator + _path.ToString();
        }



        /// <summary>
        /// 식별자의 문자열 표현을 <see cref="Identifier"/> 구조체로 구문 분석합니다.
        /// 문자열은 "namespace:path" 또는 "path" 형식일 수 있습니다 (후자의 경우 기본 네임스페이스가 사용됨).
        /// </summary>
        /// <param name="identifier">구문 분석할 문자열입니다.</param>
        /// <returns>구문 분석된 문자열을 나타내는 <see cref="Identifier"/> 구조체입니다.</returns>
        /// <exception cref="InvalidIdentifierException">식별자 문자열이 비어 있거나 형식이 유효하지 않은 경우 발생합니다.</exception>
        public static Identifier Parse(string identifier)
        {
            // 식별자 문자열을 구분자 (':')를 기준으로 분리합니다.
            var splittedSpan = identifier.AsSpan().Split(separator);

            string nameSpace = string.Empty;
            string path = string.Empty;

            // 분리된 각 부분을 순회하여 네임스페이스와 경로를 할당합니다.
            int splitCount = 0;
            foreach (var item in splittedSpan)
            {
                if (splitCount == 0)
                    nameSpace = item.AsSpan().ToString(); // 첫 번째 부분은 네임스페이스로 간주합니다.
                else if (splitCount == 1)
                    path = item.AsSpan().ToString(); // 두 번째 부분은 경로로 간주합니다.

                splitCount++;
            }

            // 분리된 부분의 개수에 따라 유효성을 검사하고 값을 조정합니다.
            if (splitCount <= 0)
                throw new InvalidIdentifierException("Identifier string cannot be empty."); // 식별자 문자열이 비어 있는 경우 예외를 발생시킵니다.
            else if (splitCount == 1)
            {
                // 구분자가 없는 경우, 전체 문자열을 경로로 간주하고 네임스페이스는 기본값으로 설정합니다.
                path = nameSpace;
                nameSpace = defaultNamespace;
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
        public static bool IsNamespaceValid(string nameSpace)
        {
            if (string.IsNullOrEmpty(nameSpace))
                return true; // 빈 문자열 또는 null은 유효한 네임스페이스로 간주 (기본값을 사용하거나 생략 가능)

            foreach (var item in nameSpace)
            {
                // 유니코드 소문자 및 숫자를 고려하지 않고, 아스키 범위로 제한
                if ((item >= 'a' && item <= 'z') || (item >= '0' && item <= '9') || item == '.' || item == '-' || item == '_')
                    continue;

                return false; // 허용되지 않는 문자를 발견하면 즉시 false 반환
            }

            return true; // 모든 문자가 유효하면 true 반환
        }

        /// <summary>
        /// 경로의 유효성을 검사합니다.
        /// 허용되는 문자: 'a-z', '0-9', '.', '/', '-', '_'
        /// </summary>
        public static bool IsPathValid(FilePath path)
        {
            if (path.IsEmpty())
                return false; // 경로는 비어있거나 null일 수 없음

            foreach (var item in path.value)
            {
                // 유니코드 소문자 및 숫자를 고려하지 않고, 아스키 범위로 제한
                if ((item >= 'a' && item <= 'z') || (item >= '0' && item <= '9') || item == '.' || item == '/' || item == '-' || item == '_')
                    continue;

                return false; // 허용되지 않는 문자를 발견하면 즉시 false 반환
            }

            return true; // 모든 문자가 유효하면 true 반환
        }
    }
}