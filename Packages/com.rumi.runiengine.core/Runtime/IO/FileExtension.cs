#nullable enable
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace RuniEngine.IO
{
    [Serializable]
    public struct FileExtension : IEquatable<FileExtension>, ISerializationCallbackReceiver
    {
        /// <summary>
        /// 표준 확장자 구분 문자로, 항상 '.'입니다.<br/>
        /// </summary>
        public const char extensionSeparatorChar = '.';

        public FileExtension(FilePath path) : this(path.value) { }
        public FileExtension(FilePath? path) : this(path?.value) { }
        public FileExtension(string? value)
        {
            _value = string.Empty;
            if (!string.IsNullOrEmpty(value))
                this.value = value;
        }

        [AllowNull]
        public string value
        {
            readonly get => _value ?? string.Empty;
            set
            {
                value ??= string.Empty;

                int index = value.LastIndexOf(extensionSeparatorChar);
                if (index >= 0)
                {
                    _value = value.Substring(index);
                    return;
                }

                _value = string.Empty;
            }
        }
        [SerializeField, FieldName("gui.value"), NotNullField, JsonIgnore] string? _value;



        public override readonly string ToString()
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value;
        }



        #region Equals
        /// <summary>
        /// 현재 <see cref="FileExtension"/> 인스턴스가 지정된 객체와 동일한지 여부를 확인합니다.<br/>
        /// 비교 대상이 <see cref="FileExtension"/> 타입이고 그 <see cref="value"/>가 현재 인스턴스의 <see cref="value"/>와 동일하면 <see langword="true"/>를 반환합니다.
        /// </summary>
        /// <param name="obj">현재 인스턴스와 비교할 객체입니다.</param>
        /// <returns>지정된 객체가 현재 <see cref="FileExtension"/>와 동일하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public override readonly bool Equals(object? obj) => obj is FileExtension path && Equals(path);

        /// <summary>
        /// 현재 <see cref="FileExtension"/> 인스턴스가 지정된 다른 <see cref="FileExtension"/> 인스턴스와 동일한지 여부를 확인합니다.<br/>
        /// 두 확장자의 <see cref="value"/> 문자열이 동일하면 <see langword="true"/>를 반환합니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 다른 <see cref="FileExtension"/> 인스턴스입니다.</param>
        /// <returns>두 <see cref="FileExtension"/> 인스턴스가 동일하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public readonly bool Equals(FileExtension other) => value == other.value;

        /// <summary>
        /// 현재 <see cref="FileExtension"/> 인스턴스가 지정된 nullable <see cref="FileExtension"/> 인스턴스와 동일한지 여부를 확인합니다.<br/>
        /// <paramref name="other"/>가 <see langword="null"/>이면 빈 확장자(<see cref="string.Empty"/>)와 비교합니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 nullable <see cref="FileExtension"/> 인스턴스입니다.</param>
        /// <returns>두 <see cref="FileExtension"/> 인스턴스(또는 <see langword="null"/> 처리된 빈 확장자)가 동일하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public readonly bool Equals(FileExtension? other) => value == (other?.value ?? string.Empty);
        #endregion



        /// <summary>
        /// 현재 <see cref="FileExtension"/> 인스턴스의 해시 코드를 반환합니다.<br/>
        /// 해시 코드는 <see cref="value"/> 문자열의 해시 코드와 동일합니다.
        /// </summary>
        /// <returns>현재 <see cref="FileExtension"/> 인스턴스의 해시 코드입니다.</returns>
        public override readonly int GetHashCode() => value.GetHashCode();



        #region operators
        /// <summary>
        /// <see cref="FileExtension"/>를 <see cref="string"/>으로 암시적으로 변환합니다.<br/>
        /// 이는 <see cref="value"/> 속성을 반환합니다.
        /// </summary>
        /// <param name="extension">변환할 <see cref="FileExtension"/> 인스턴스입니다.</param>
        public static implicit operator string(FileExtension extension) => extension.value;

        /// <summary>
        /// nullable <see cref="FileExtension"/>를 <see cref="string"/>으로 암시적으로 변환합니다.<br/>
        /// <paramref name="extension"/>가 <see langword="null"/>이면 <see cref="string.Empty"/>를 반환하고, 그렇지 않으면 <see cref="value"/>를 반환합니다.
        /// </summary>
        /// <param name="extension">변환할 nullable <see cref="FileExtension"/> 인스턴스입니다.</param>
        public static implicit operator string(FileExtension? extension) => extension?.value ?? string.Empty;

        /// <summary>
        /// <see cref="string"/>을 <see cref="FileExtension"/>로 암시적으로 변환합니다.<br/>
        /// </summary>
        /// <param name="extension">변환할 문자열 확장자입니다.</param>
        public static implicit operator FileExtension(string? extension) => new FileExtension(extension);
        #endregion



        public void OnBeforeSerialize() => value = value;
        public void OnAfterDeserialize() => value = value;
    }
}
