#nullable enable
using RuniOS.Texts.Styles;

namespace RuniOS.Texts
{
    /// <summary>
    /// Represents a styled text element that can be resolved or rendered by text pipelines.<br/>
    /// 텍스트 파이프라인에서 해석되거나 렌더링될 수 있는 스타일 적용 텍스트 요소를 나타냅니다.
    /// </summary>
    public abstract partial class Text
    {
        /// <summary>
        /// Gets the style assigned to this text, if any.<br/>
        /// 이 텍스트에 할당된 스타일이 있으면 가져옵니다.
        /// </summary>
        public TextStyle? style { get; private set; }

        /// <summary>
        /// Sets the value of the specified style property on this text.<br/>
        /// 이 텍스트에 지정된 스타일 속성 값을 설정합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The value type stored by the style property.<br/>
        /// 스타일 속성이 저장하는 값 타입입니다.
        /// </typeparam>
        /// <param name="property">
        /// The style property to set.<br/>
        /// 설정할 스타일 속성입니다.
        /// </param>
        /// <param name="value">
        /// The value to assign to the style property.<br/>
        /// 스타일 속성에 할당할 값입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text SetStyle<T>(StyleKey<T> property, T value)
        {
            (style ??= new TextStyle()).Set(property, value);
            return this;
        }

        /// <summary>
        /// Removes the value assigned to the specified style property from this text.<br/>
        /// 이 텍스트에서 지정된 스타일 속성에 할당된 값을 제거합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The value type stored by the style property.<br/>
        /// 스타일 속성이 저장하는 값 타입입니다.
        /// </typeparam>
        /// <param name="property">
        /// The style property to unset.<br/>
        /// 제거할 스타일 속성입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text UnsetStyle<T>(StyleKey<T> property)
        {
            style?.Unset(property);
            return this;
        }

        /// <summary>
        /// Removes all style values assigned to this text.<br/>
        /// 이 텍스트에 할당된 모든 스타일 값을 제거합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text ClearStyle()
        {
            style?.Clear();
            return this;
        }

        /// <summary>
        /// Replaces this text's style with a clone of another text's style.<br/>
        /// 이 텍스트의 스타일을 다른 텍스트 스타일의 복제본으로 교체합니다.
        /// </summary>
        /// <param name="target">
        /// The text whose style should be copied.<br/>
        /// 복사할 스타일을 가진 텍스트입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text CopyStyleFrom(Text target)
        {
            style = target.style?.Clone();
            return this;
        }
    }
}
