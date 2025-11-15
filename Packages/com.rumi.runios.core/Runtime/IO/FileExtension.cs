#nullable enable
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.IO
{
    /// <summary>
    /// 파일 확장자를 나타내는 구조체입니다.<br/>
    /// 확장자는 항상 '.'으로 시작하며, 경로 문자열에서 확장자 부분만 파싱하여 관리합니다.<br/>
    /// 경로의 마지막 '.' 이후 문자열만 확장자로 간주합니다.
    /// </summary>
    [Serializable]
    public struct FileExtension : IEquatable<FileExtension>, ISerializationCallbackReceiver
    {
        /// <summary>
        /// 표준 확장자 구분 문자로, 항상 '.'입니다.<br/>
        /// </summary>
        public const char extensionSeparatorChar = '.';
        
        /// <summary>
        /// 빈 파일 확장자를 나타내는 정적 읽기 전용 인스턴스입니다.<br/>
        /// <c>new FileExtension()</c>와 동일하며, 확장자가 없는 상태를 표현할 때 사용됩니다.
        /// </summary>
        public static readonly FileExtension empty = new FileExtension();

        /// <summary>
        /// <see cref="FilePath"/>에서 파일 확장자를 초기화합니다.
        /// </summary>
        /// <param name="path">확장자를 추출할 <see cref="FilePath"/>입니다.</param>
        public FileExtension(FilePath path) : this(path.value) { }
        
        /// <summary>
        /// nullable <see cref="FilePath"/>에서 파일 확장자를 초기화합니다.<br/>
        /// <paramref name="path"/>가 <see langword="null"/>이면 빈 확장자로 초기화됩니다.
        /// </summary>
        /// <param name="path">확장자를 추출할 nullable <see cref="FilePath"/>입니다.</param>
        public FileExtension(FilePath? path) : this(path?.value) { }
        
        /// <summary>
        /// 문자열 값에서 파일 확장자를 초기화합니다.<br/>
        /// 제공된 문자열에서 마지막 '.' 이후의 부분을 확장자로 설정합니다.<br/>
        /// 만약 '.'이 없거나 <see langword="null"/> 또는 비어있는 문자열이 제공되면 빈 확장자로 초기화됩니다.
        /// </summary>
        /// <param name="value">확장자를 포함할 수 있는 문자열 값입니다.</param>
        public FileExtension(string? value)
        {
            _value = string.Empty;
            if (!string.IsNullOrEmpty(value))
                this.value = value;
        }

        /// <summary>
        /// 파일 확장자의 실제 문자열 값을 가져오거나 설정합니다.<br/>
        /// 설정 시, 값에서 마지막 '.' 이후의 문자열만 확장자로 추출하여 저장합니다.<br/>
        /// 예를 들어, ".png", "jpg", "image.gif" 등의 값이 주어지면 각각 ".png", "", ".gif"로 저장됩니다.<br/>
        /// 만약 '.'이 없으면 빈 문자열로 설정됩니다.
        /// </summary>
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



        /// <summary>
        /// 현재 <see cref="FileExtension"/> 인스턴스의 문자열 표현을 반환합니다.<br/>
        /// 이는 <see cref="value"/> 속성과 동일합니다.
        /// </summary>
        /// <returns>현재 <see cref="FileExtension"/>의 문자열 값입니다.</returns>
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



        /// <summary>
        /// 직렬화 전에 호출됩니다.<br/>
        /// <see cref="value"/> 속성을 재설정하여 올바른 확장자 형식을 유지하도록 합니다.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize() => value = value;
        
        /// <summary>
        /// 역직렬화 후에 호출됩니다.<br/>
        /// <see cref="value"/> 속성을 재설정하여 올바른 확장자 형식을 유지하도록 합니다.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize() => value = value;
    }
}