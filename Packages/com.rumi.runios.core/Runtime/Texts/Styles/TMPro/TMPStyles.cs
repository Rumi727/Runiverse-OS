#nullable enable
namespace RuniOS.Texts.Styles.TMPro
{
    /// <summary>
    /// Defines TextMesh Pro rich-text style keys.<br/>
    /// TextMesh Pro rich text 스타일 키를 정의합니다.
    /// </summary>
    public static class TMPStyles
    {
        /// <summary>
        /// The style key for horizontal alignment.<br/>
        /// 가로 정렬용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<TextAlign> align = "align";
        /// <summary>
        /// The style key for alpha transparency.<br/>
        /// 알파 투명도용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<float> alpha = "alpha";
        /// <summary>
        /// The style key for character spacing.<br/>
        /// 글자 간격용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<Length> characterSpacing = "cspace";
        /// <summary>
        /// The style key for font asset name.<br/>
        /// 글꼴 에셋 이름용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<string> font = "font";
        /// <summary>
        /// The style key for font weight.<br/>
        /// 글꼴 굵기용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<int> fontWeight = "font-weight";
        /// <summary>
        /// The style key for gradient preset name.<br/>
        /// 그라디언트 프리셋 이름용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<string> gradient = "gradient";
        /// <summary>
        /// The style key for indentation.<br/>
        /// 들여쓰기용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<Length> indent = "indent";
        /// <summary>
        /// The style key for line height.<br/>
        /// 줄 높이용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<Length> lineHeight = "line-height";
        /// <summary>
        /// The style key for first-line indentation.<br/>
        /// 첫 줄 들여쓰기용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<Length> lineIndent = "line-indent";
        /// <summary>
        /// The style key for lowercase conversion.<br/>
        /// 소문자 변환용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<bool> lowercase = "lowercase";
        /// <summary>
        /// The style key for text margin.<br/>
        /// 텍스트 여백용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<Length> margin = "margin";
        /// <summary>
        /// The style key for monospace spacing.<br/>
        /// 고정폭 간격용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<Length> monoSpacing = "mspace";
        /// <summary>
        /// The style key for disabling line breaks.<br/>
        /// 줄바꿈 방지용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<bool> noBreak = "nobr";
        /// <summary>
        /// The style key for horizontal position offset.<br/>
        /// 가로 위치 오프셋용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<Length> position = "pos";
        /// <summary>
        /// The style key for text rotation.<br/>
        /// 텍스트 회전용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<float> rotation = "rotate";
        /// <summary>
        /// The style key for small-caps conversion.<br/>
        /// 작은 대문자 변환용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<bool> smallcaps = "smallcaps";
        /// <summary>
        /// The style key for subscript text.<br/>
        /// 아래 첨자 텍스트용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<bool> subscript = "sub";
        /// <summary>
        /// The style key for superscript text.<br/>
        /// 위 첨자 텍스트용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<bool> superscript = "sup";
        /// <summary>
        /// The style key for uppercase conversion.<br/>
        /// 대문자 변환용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<bool> uppercase = "uppercase";
        /// <summary>
        /// The style key for vertical offset.<br/>
        /// 세로 오프셋용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<Length> verticalOffset = "voffset";
        /// <summary>
        /// The style key for text width.<br/>
        /// 텍스트 너비용 스타일 키입니다.
        /// </summary>
        public static readonly StyleKey<Length> width = "width";
    }
}
