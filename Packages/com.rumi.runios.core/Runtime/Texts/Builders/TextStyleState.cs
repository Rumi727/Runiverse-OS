#nullable enable
using RuniOS.Texts.Styles;

namespace RuniOS.Texts.Builders
{
    /// <summary>
    /// Tracks nested text style values while a builder writes text output.<br/>
    /// 빌더가 텍스트 출력을 작성하는 동안 중첩된 텍스트 스타일 값을 추적합니다.
    /// </summary>
    public sealed class TextStyleState
    {
        readonly Dictionary<string, List<object?>> values = [];
        readonly Stack<TextStyle> stack = new Stack<TextStyle>();

        /// <summary>
        /// Gets the style at the top of the current style stack.<br/>
        /// 현재 스타일 스택 맨 위의 스타일을 가져옵니다.
        /// </summary>
        public TextStyle current => stack.Peek();

        /// <summary>
        /// Pushes a style onto the stack and makes its values current.<br/>
        /// 스타일을 스택에 넣고 해당 값을 현재 값으로 만듭니다.
        /// </summary>
        /// <param name="style">
        /// The style to open.<br/>
        /// 열 스타일입니다.
        /// </param>
        public void Open(TextStyle style)
        {
            stack.Push(style);

            foreach (var styleItem in style)
            {
                if (!values.TryGetValue(styleItem.Key, out var valueList))
                {
                    valueList = [];
                    values[styleItem.Key] = valueList;
                }

                valueList.Add(styleItem.Value);
            }
        }

        /// <summary>
        /// Pops the current style from the stack.<br/>
        /// 현재 스타일을 스택에서 제거합니다.
        /// </summary>
        public void Close()
        {
            foreach (var styleItem in stack.Pop())
            {
                if (values.TryGetValue(styleItem.Key, out var valueList) && valueList.Count > 0)
                    valueList.RemoveAt(valueList.Count - 1);
            }
        }

        const int MAX_VALUE_COUNT = 16;
        /// <summary>
        /// Clears all tracked style state for reuse.<br/>
        /// 재사용을 위해 추적 중인 모든 스타일 상태를 지웁니다.
        /// </summary>
        public void Clear()
        {
            if (values.Count > MAX_VALUE_COUNT)
                values.Clear();
            else
            {
                foreach (var item in values.Values)
                    item.Clear();
            }

            stack.Clear();
        }

        /// <summary>
        /// Gets the current value for the specified style key.<br/>
        /// 지정된 스타일 키의 현재 값을 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The value type associated with the style key.<br/>
        /// 스타일 키와 연결된 값 타입입니다.
        /// </typeparam>
        /// <param name="key">
        /// The style key to read.<br/>
        /// 읽을 스타일 키입니다.
        /// </param>
        /// <returns>
        /// The current style value, or an empty optional value when no matching value is active.<br/>
        /// 현재 스타일 값이며, 활성화된 일치 값이 없으면 빈 optional 값을 반환합니다.
        /// </returns>
        public StyleProperty<T> Get<T>(StyleKey<T> key)
        {
            if (values.TryGetValue(key, out var valueList))
            {
                if (valueList.Count > 0 && valueList[^1] is T genericValue)
                    return genericValue;
            }

            return default;
        }

        /// <summary>
        /// Gets the parent value for the specified style key.<br/>
        /// 지정된 스타일 키의 부모 값을 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The value type associated with the style key.<br/>
        /// 스타일 키와 연결된 값 타입입니다.
        /// </typeparam>
        /// <param name="key">
        /// The style key to read.<br/>
        /// 읽을 스타일 키입니다.
        /// </param>
        /// <returns>
        /// The parent style value, or an empty optional value when no matching parent value is active.<br/>
        /// 부모 스타일 값이며, 활성화된 일치 부모 값이 없으면 빈 optional 값을 반환합니다.
        /// </returns>
        public StyleProperty<T> GetParent<T>(StyleKey<T> key)
        {
            if (values.TryGetValue(key, out var valueList))
            {
                if (valueList.Count > 1 && valueList[^2] is T genericValue)
                    return genericValue;
            }

            return default;
        }
    }
}
