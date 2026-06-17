#nullable enable
namespace RuniOS.Texts.Styles
{
    /// <summary>
    /// Defines common text style keys shared by text renderers.<br/>
    /// 텍스트 렌더러가 공유하는 공통 텍스트 스타일 키를 정의합니다.
    /// </summary>
    public static class TextStyles
    {
        /// <summary>
        /// The style key for bold text.<br/>
        /// 굵게 텍스트용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<bool> bold = "b";
        /// <summary>
        /// The style key for foreground color.<br/>
        /// 전경색용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<HexColor> color = "color";
        /// <summary>
        /// The style key for italic text.<br/>
        /// 기울임 텍스트용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<bool> italic = "i";
        /// <summary>
        /// The style key for mark color.<br/>
        /// 마크 색상용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<HexColor> mark = "mark";
        /// <summary>
        /// The style key for text size.<br/>
        /// 텍스트 크기용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<Length> size = "size";
        /// <summary>
        /// The style key for strikethrough text.<br/>
        /// 취소선 텍스트용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<bool> strikethrough = "s";
        /// <summary>
        /// The style key for underlined text.<br/>
        /// 밑줄 텍스트용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<bool> underline = "u";
    }
}
