#nullable enable
using RuniOS.Texts.Styles;

namespace RuniOS.Texts
{
    public partial class TextExtension
    {
        extension(Text text)
        {
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
            public Text Bold(bool value = true) => text.SetStyle(TextStyles.bold, value);

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
            public Text Italic(bool value = true) => text.SetStyle(TextStyles.italic, value);

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
            public Text Strikethrough(bool value = true) => text.SetStyle(TextStyles.strikethrough, value);

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
            public Text Underline(bool value = true) => text.SetStyle(TextStyles.underline, value);

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
            public Text Color(HexColor value) => text.SetStyle(TextStyles.color, value);

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
            public Text Mark(HexColor value) => text.SetStyle(TextStyles.mark, value);

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
            public Text Size(Length value) => text.SetStyle(TextStyles.size, value);

            /// <summary>
            /// Sets the foreground color to black.<br/>
            /// 전경색을 검은색으로 설정합니다.
            /// </summary>
            /// <returns>
            /// This text instance.<br/>
            /// 이 텍스트 인스턴스를 반환합니다.
            /// </returns>
            public Text Black() => text.SetStyle(TextStyles.color, HexColor.black);

            /// <summary>
            /// Sets the foreground color to blue.<br/>
            /// 전경색을 파란색으로 설정합니다.
            /// </summary>
            /// <returns>
            /// This text instance.<br/>
            /// 이 텍스트 인스턴스를 반환합니다.
            /// </returns>
            public Text Blue() => text.SetStyle(TextStyles.color, HexColor.blue);

            /// <summary>
            /// Sets the foreground color to green.<br/>
            /// 전경색을 초록색으로 설정합니다.
            /// </summary>
            /// <returns>
            /// This text instance.<br/>
            /// 이 텍스트 인스턴스를 반환합니다.
            /// </returns>
            public Text Green() => text.SetStyle(TextStyles.color, HexColor.green);

            /// <summary>
            /// Sets the foreground color to orange.<br/>
            /// 전경색을 주황색으로 설정합니다.
            /// </summary>
            /// <returns>
            /// This text instance.<br/>
            /// 이 텍스트 인스턴스를 반환합니다.
            /// </returns>
            public Text Orange() => text.SetStyle(TextStyles.color, HexColor.orange);

            /// <summary>
            /// Sets the foreground color to purple.<br/>
            /// 전경색을 보라색으로 설정합니다.
            /// </summary>
            /// <returns>
            /// This text instance.<br/>
            /// 이 텍스트 인스턴스를 반환합니다.
            /// </returns>
            public Text Purple() => text.SetStyle(TextStyles.color, HexColor.purple);

            /// <summary>
            /// Sets the foreground color to red.<br/>
            /// 전경색을 빨간색으로 설정합니다.
            /// </summary>
            /// <returns>
            /// This text instance.<br/>
            /// 이 텍스트 인스턴스를 반환합니다.
            /// </returns>
            public Text Red() => text.SetStyle(TextStyles.color, HexColor.red);

            /// <summary>
            /// Sets the foreground color to white.<br/>
            /// 전경색을 흰색으로 설정합니다.
            /// </summary>
            /// <returns>
            /// This text instance.<br/>
            /// 이 텍스트 인스턴스를 반환합니다.
            /// </returns>
            public Text White() => text.SetStyle(TextStyles.color, HexColor.white);

            /// <summary>
            /// Sets the foreground color to yellow.<br/>
            /// 전경색을 노란색으로 설정합니다.
            /// </summary>
            /// <returns>
            /// This text instance.<br/>
            /// 이 텍스트 인스턴스를 반환합니다.
            /// </returns>
            public Text Yellow() => text.SetStyle(TextStyles.color, HexColor.yellow);
        }
    }
}