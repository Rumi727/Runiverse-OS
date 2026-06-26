#nullable enable
using System.Runtime.CompilerServices;

namespace RuniOS.Texts
{
    /// <summary>
    /// Collects interpolated string segments as a <see cref="GroupText"/> instance.<br/>
    /// 보간 문자열 세그먼트를 <see cref="GroupText"/> 인스턴스로 수집합니다.
    /// </summary>
    [InterpolatedStringHandler]
    public readonly ref struct GroupTextStringHandler
    {
        readonly GroupText groupText;

        // ReSharper disable UnusedParameter.Local
        /// <summary>
        /// Initializes a handler for a grouped text interpolated string.<br/>
        /// 그룹 텍스트 보간 문자열용 핸들러를 초기화합니다.
        /// </summary>
        /// <param name="literalLength">
        /// The total length of literal content in the interpolated string.<br/>
        /// 보간 문자열에 포함된 리터럴 콘텐츠의 전체 길이입니다.
        /// </param>
        /// <param name="formattedCount">
        /// The number of formatted segments in the interpolated string.<br/>
        /// 보간 문자열에 포함된 format 세그먼트 수입니다.
        /// </param>
        public GroupTextStringHandler(int literalLength, int formattedCount) => groupText = Text.Group();
        // ReSharper restore UnusedParameter.Local

        /// <summary>
        /// Appends a literal string segment.<br/>
        /// 리터럴 문자열 세그먼트를 추가합니다.
        /// </summary>
        /// <param name="value">
        /// The literal string segment to append.<br/>
        /// 추가할 리터럴 문자열 세그먼트입니다.
        /// </param>
        public void AppendLiteral(string value)
        {
            if (value.Length > 0)
                groupText.Add(Text.Literal(value));
        }

        /// <summary>
        /// Appends an existing text segment.<br/>
        /// 기존 텍스트 세그먼트를 추가합니다.
        /// </summary>
        /// <param name="value">
        /// The text segment to append.<br/>
        /// 추가할 텍스트 세그먼트입니다.
        /// </param>
        public void AppendFormatted(Text value) => groupText.Add(value);

        /// <summary>
        /// Appends an interpolated value as a <see cref="ValueText{T}"/> segment.<br/>
        /// 보간 값을 <see cref="ValueText{T}"/> 세그먼트로 추가합니다.
        /// </summary>
        /// <param name="value">
        /// The value to append.<br/>
        /// 추가할 값입니다.
        /// </param>
        public void AppendFormatted(object? value) => groupText.Add(Text.Value(value));

        /// <summary>
        /// Appends an interpolated value with alignment information.<br/>
        /// 정렬 정보를 가진 보간 값을 추가합니다.
        /// </summary>
        /// <param name="value">
        /// The value to append.<br/>
        /// 추가할 값입니다.
        /// </param>
        /// <param name="alignment">
        /// The composite-format alignment width to apply when formatting.<br/>
        /// format 처리 시 적용할 복합 format 정렬 너비입니다.
        /// </param>
        public void AppendFormatted(object? value, int alignment) => groupText.Add(Text.Value(value, alignment));

        /// <summary>
        /// Appends an interpolated value with optional format information.<br/>
        /// 선택적 format 정보를 가진 보간 값을 추가합니다.
        /// </summary>
        /// <param name="value">
        /// The value to append.<br/>
        /// 추가할 값입니다.
        /// </param>
        /// <param name="format">
        /// The format string to apply, or <see langword="null"/> to append without a format string.<br/>
        /// 적용할 format 문자열이며, <see langword="null"/>이면 format 문자열 없이 추가합니다.
        /// </param>
        public void AppendFormatted(object? value, string? format)
        {
            Text text;
            if (format != null)
                text = Text.Value(value, format);
            else
                text = Text.Value(value);

            groupText.Add(text);
        }

        /// <summary>
        /// Appends an interpolated value with alignment and optional format information.<br/>
        /// 정렬 및 선택적 format 정보를 가진 보간 값을 추가합니다.
        /// </summary>
        /// <param name="value">
        /// The value to append.<br/>
        /// 추가할 값입니다.
        /// </param>
        /// <param name="alignment">
        /// The composite-format alignment width to apply when formatting.<br/>
        /// format 처리 시 적용할 복합 format 정렬 너비입니다.
        /// </param>
        /// <param name="format">
        /// The format string to apply, or <see langword="null"/> to append without a format string.<br/>
        /// 적용할 format 문자열이며, <see langword="null"/>이면 format 문자열 없이 추가합니다.
        /// </param>
        public void AppendFormatted(object? value, int alignment, string? format)
        {
            Text text;
            if (format != null)
                text = Text.Value(value, alignment, format);
            else
                text = Text.Value(value, alignment);

            groupText.Add(text);
        }

        /// <summary>
        /// Gets the collected grouped text.<br/>
        /// 수집된 그룹 텍스트를 가져옵니다.
        /// </summary>
        /// <returns>
        /// The grouped text collected by this handler.<br/>
        /// 이 핸들러가 수집한 그룹 텍스트를 반환합니다.
        /// </returns>
        public GroupText ToGroupText() => groupText;
    }
}
