#nullable enable
using System.Collections;

namespace RuniOS.Texts.Styles
{
    /// <summary>
    /// Stores style values by string-backed typed style properties.<br/>
    /// 문자열 기반 타입 지정 스타일 속성으로 스타일 값을 저장합니다.
    /// </summary>
    public sealed class TextStyle : IReadOnlyCollection<KeyValuePair<string, object?>>, ICloneable
    {
        /// <summary>
        /// Initializes an empty text style.<br/>
        /// 빈 텍스트 스타일을 초기화합니다.
        /// </summary>
        public TextStyle() { }
        TextStyle(Dictionary<string, object?> styles) => this.styles = styles.ToDictionary(x => x.Key, x => x.Value);

        readonly Dictionary<string, object?> styles = [];

        /// <summary>
        /// Gets the number of style values stored in this style.<br/>
        /// 이 스타일에 저장된 스타일 값 수를 가져옵니다.
        /// </summary>
        public int count => styles.Count;

        /// <inheritdoc/>
        int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => count;

        /// <summary>
        /// Gets the value assigned to the specified style property.<br/>
        /// 지정된 스타일 속성에 할당된 값을 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The value type stored by the style property.<br/>
        /// 스타일 속성이 저장하는 값 타입입니다.
        /// </typeparam>
        /// <param name="key">
        /// The style property to read.<br/>
        /// 읽을 스타일 속성입니다.
        /// </param>
        /// <returns>
        /// The optional style value, or an empty optional value when the property is not set or has a different type.<br/>
        /// 스타일 값이 있으면 해당 값을, 속성이 없거나 타입이 다른 경우 빈 optional 값을 반환합니다.
        /// </returns>
        public StyleProperty<T> Get<T>(StyleKey<T> key)
        {
            if (styles.TryGetValue(key, out object? value) && value is T genericValue)
                return genericValue;

            return default;
        }

        /// <summary>
        /// Sets the value of the specified style property.<br/>
        /// 지정된 스타일 속성 값을 설정합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The value type stored by the style property.<br/>
        /// 스타일 속성이 저장하는 값 타입입니다.
        /// </typeparam>
        /// <param name="key">
        /// The style property to set.<br/>
        /// 설정할 스타일 속성입니다.
        /// </param>
        /// <param name="value">
        /// The value to assign.<br/>
        /// 할당할 값입니다.
        /// </param>
        public void Set<T>(StyleKey<T> key, T value) => styles[key] = value;

        /// <summary>
        /// Removes the value assigned to the specified style property.<br/>
        /// 지정된 스타일 속성에 할당된 값을 제거합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The value type stored by the style property.<br/>
        /// 스타일 속성이 저장하는 값 타입입니다.
        /// </typeparam>
        /// <param name="key">
        /// The style property to remove.<br/>
        /// 제거할 스타일 속성입니다.
        /// </param>
        public void Unset<T>(StyleKey<T> key) => styles.Remove(key);

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => styles.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)styles).GetEnumerator();

        /// <summary>
        /// Creates a shallow clone of this text style.<br/>
        /// 이 텍스트 스타일의 얕은 복제본을 만듭니다.
        /// </summary>
        /// <returns>
        /// A new text style containing the same property values.<br/>
        /// 같은 속성 값을 포함하는 새 텍스트 스타일을 반환합니다.
        /// </returns>
        public TextStyle Clone() => new TextStyle(styles);

        /// <inheritdoc/>
        object ICloneable.Clone() => Clone();
    }
}
