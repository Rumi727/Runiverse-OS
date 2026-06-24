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

        /// <summary>
        /// Sets or clears bold styling.<br/>
        /// 굵게 스타일을 설정하거나 해제합니다.
        /// </summary>
        /// <param name="value">
        /// <see langword="true"/> to enable bold styling; <see langword="false"/> to disable it.<br/>
        /// 굵게 스타일을 켜려면 <see langword="true"/>, 끄려면 <see langword="false"/>입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Bold(bool value = true) => SetStyle(TextStyles.bold, value);

        /// <summary>
        /// Sets or clears italic styling.<br/>
        /// 기울임 스타일을 설정하거나 해제합니다.
        /// </summary>
        /// <param name="value">
        /// <see langword="true"/> to enable italic styling; <see langword="false"/> to disable it.<br/>
        /// 기울임 스타일을 켜려면 <see langword="true"/>, 끄려면 <see langword="false"/>입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Italic(bool value = true) => SetStyle(TextStyles.italic, value);

        /// <summary>
        /// Sets or clears strikethrough styling.<br/>
        /// 취소선 스타일을 설정하거나 해제합니다.
        /// </summary>
        /// <param name="value">
        /// <see langword="true"/> to enable strikethrough styling; <see langword="false"/> to disable it.<br/>
        /// 취소선 스타일을 켜려면 <see langword="true"/>, 끄려면 <see langword="false"/>입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Strikethrough(bool value = true) => SetStyle(TextStyles.strikethrough, value);

        /// <summary>
        /// Sets or clears underline styling.<br/>
        /// 밑줄 스타일을 설정하거나 해제합니다.
        /// </summary>
        /// <param name="value">
        /// <see langword="true"/> to enable underline styling; <see langword="false"/> to disable it.<br/>
        /// 밑줄 스타일을 켜려면 <see langword="true"/>, 끄려면 <see langword="false"/>입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Underline(bool value = true) => SetStyle(TextStyles.underline, value);

        /// <summary>
        /// Sets the foreground color style.<br/>
        /// 전경색 스타일을 설정합니다.
        /// </summary>
        /// <param name="value">
        /// The color to assign.<br/>
        /// 할당할 색상입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Color(HexColor value) => SetStyle(TextStyles.color, value);

        /// <summary>
        /// Sets the mark color style.<br/>
        /// 마크 색상 스타일을 설정합니다.
        /// </summary>
        /// <param name="value">
        /// The mark color to assign.<br/>
        /// 할당할 마크 색상입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Mark(HexColor value) => SetStyle(TextStyles.mark, value);

        /// <summary>
        /// Sets the text size style.<br/>
        /// 텍스트 크기 스타일을 설정합니다.
        /// </summary>
        /// <param name="value">
        /// The size value to assign.<br/>
        /// 할당할 크기 값입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Size(Length value) => SetStyle(TextStyles.size, value);

        /// <summary>
        /// Sets the foreground color to black.<br/>
        /// 전경색을 검은색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Black() => SetStyle(TextStyles.color, HexColor.black);

        /// <summary>
        /// Sets the foreground color to blue.<br/>
        /// 전경색을 파란색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Blue() => SetStyle(TextStyles.color, HexColor.blue);

        /// <summary>
        /// Sets the foreground color to green.<br/>
        /// 전경색을 초록색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Green() => SetStyle(TextStyles.color, HexColor.green);

        /// <summary>
        /// Sets the foreground color to orange.<br/>
        /// 전경색을 주황색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Orange() => SetStyle(TextStyles.color, HexColor.orange);

        /// <summary>
        /// Sets the foreground color to purple.<br/>
        /// 전경색을 보라색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Purple() => SetStyle(TextStyles.color, HexColor.purple);

        /// <summary>
        /// Sets the foreground color to red.<br/>
        /// 전경색을 빨간색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Red() => SetStyle(TextStyles.color, HexColor.red);

        /// <summary>
        /// Sets the foreground color to white.<br/>
        /// 전경색을 흰색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text White() => SetStyle(TextStyles.color, HexColor.white);

        /// <summary>
        /// Sets the foreground color to yellow.<br/>
        /// 전경색을 노란색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Yellow() => SetStyle(TextStyles.color, HexColor.yellow);
    }
}
