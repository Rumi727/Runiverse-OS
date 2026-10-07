#nullable enable
namespace RuniOS.Texts.Styles
{
    /// <summary>
    /// Represents a typed key used to store and retrieve a text style value.<br/>
    /// 텍스트 스타일 값을 저장하고 가져올 때 사용하는 타입 지정 키를 나타냅니다.
    /// </summary>
    /// <typeparam name="T">
    /// The value type associated with this style property.<br/>
    /// 이 스타일 속성과 연결된 값 타입입니다.
    /// </typeparam>
    /// <param name="key">
    /// The unique string key for the style property.<br/>
    /// 스타일 속성의 고유 문자열 키입니다.
    /// </param>
    public readonly record struct StyleKey<T>(string key)
    {
        /// <summary>
        /// Creates a typed style property from a string key.<br/>
        /// 문자열 키에서 타입 지정 스타일 속성을 만듭니다.
        /// </summary>
        /// <param name="key">
        /// The style property key.<br/>
        /// 스타일 속성 키입니다.
        /// </param>
        public static implicit operator StyleKey<T>(string key) => new StyleKey<T>(key);

        /// <summary>
        /// Gets the string key from a typed style property.<br/>
        /// 타입 지정 스타일 속성에서 문자열 키를 가져옵니다.
        /// </summary>
        /// <param name="property">
        /// The style property to convert.<br/>
        /// 변환할 스타일 속성입니다.
        /// </param>
        public static implicit operator string(StyleKey<T> property) => property.key;

        /// <summary>
        /// Gets the string key represented by this style key.<br/>
        /// 이 스타일 키가 나타내는 문자열 키를 가져옵니다.
        /// </summary>
        /// <returns>
        /// The string key.<br/>
        /// 문자열 키를 반환합니다.
        /// </returns>
        public override string ToString() => key;
    }
}
