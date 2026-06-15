#nullable enable

namespace RuniOS.Formats
{
    /// <summary>
    /// Represents one parsed segment of a composite format string.<br/>
    /// composite format 문자열에서 파싱된 하나의 세그먼트를 나타냅니다.
    /// </summary>
    /// <remarks>
    /// A segment represents either literal text or a format hole.<br/>
    /// Literal segments have a non-<see langword="null"/> <see cref="literal"/> value and an <see cref="index"/> value of -1.<br/>
    /// Format hole segments have a <see langword="null"/> <see cref="literal"/> value and a non-negative <see cref="index"/> value.
    /// <br/><br/>
    /// 세그먼트는 literal 텍스트 또는 format hole 중 하나를 나타냅니다.<br/>
    /// literal 세그먼트는 <see cref="literal"/> 값이 <see langword="null"/>이 아니고 <see cref="index"/> 값이 -1입니다.<br/>
    /// format hole 세그먼트는 <see cref="literal"/> 값이 <see langword="null"/>이고 <see cref="index"/> 값이 0 이상입니다.
    /// </remarks>
    public readonly record struct CompositeFormatSegment
    {
        /// <summary>
        /// Initializes a literal segment with the specified text.<br/>
        /// 지정된 텍스트를 사용하여 literal 세그먼트를 초기화합니다.
        /// </summary>
        /// <param name="literal">
        /// The literal text represented by the segment.<br/>
        /// 세그먼트가 나타내는 literal 텍스트입니다.
        /// </param>
        public CompositeFormatSegment(string? literal)
        {
            this.literal = literal;
            index = -1;
        }

        /// <summary>
        /// Initializes a format hole segment with the specified argument index, alignment, and format string.<br/>
        /// 지정된 인수 인덱스, 정렬 값, format 문자열을 사용하여 format hole 세그먼트를 초기화합니다.
        /// </summary>
        /// <param name="index">
        /// The zero-based argument index referenced by the format hole.<br/>
        /// format hole이 참조하는 0부터 시작하는 인수 인덱스입니다.
        /// </param>
        /// <param name="alignment">
        /// The alignment width parsed from the format hole, or 0 when no alignment is specified.<br/>
        /// format hole에서 파싱된 정렬 너비이며, 정렬 값이 지정되지 않은 경우 0입니다.
        /// </param>
        /// <param name="format">
        /// The custom format string parsed from the format hole, or <see langword="null"/> when no format is specified.<br/>
        /// format hole에서 파싱된 사용자 지정 format 문자열이며, format이 지정되지 않은 경우 <see langword="null"/>입니다.
        /// </param>
        public CompositeFormatSegment(int index, int alignment, string? format)
        {
            this.index = index;
            this.alignment = alignment;
            this.format = format;
        }

        /// <summary>
        /// Gets the literal text represented by this segment.<br/>
        /// 이 세그먼트가 나타내는 literal 텍스트를 가져옵니다.
        /// </summary>
        public string? literal { get; init; }

        /// <summary>
        /// Gets the zero-based argument index referenced by this segment.<br/>
        /// 이 세그먼트가 참조하는 0부터 시작하는 인수 인덱스를 가져옵니다.
        /// </summary>
        public int index { get; init; }
        
        /// <summary>
        /// Gets the alignment width represented by this segment.<br/>
        /// 이 세그먼트가 나타내는 정렬 너비를 가져옵니다.
        /// </summary>
        public int alignment { get; init; }
        
        /// <summary>
        /// Gets the custom format string represented by this segment.<br/>
        /// 이 세그먼트가 나타내는 사용자 지정 format 문자열을 가져옵니다.
        /// </summary>
        public string? format { get; init; }

        /// <summary>
        /// Gets a value indicating whether this segment represents literal text.<br/>
        /// 이 세그먼트가 literal 텍스트를 나타내는지 여부를 가져옵니다.
        /// </summary>
        public bool isLiteral => literal != null;

        /// <summary>
        /// Deconstructs this segment into its literal text, argument index, alignment, and format string.<br/>
        /// 이 세그먼트를 literal 텍스트, 인수 인덱스, 정렬 값, format 문자열로 분해합니다.
        /// </summary>
        /// <param name="literal">
        /// The literal text represented by this segment.<br/>
        /// 이 세그먼트가 나타내는 literal 텍스트입니다.
        /// </param>
        /// <param name="index">
        /// The zero-based argument index referenced by this segment.<br/>
        /// 이 세그먼트가 참조하는 0부터 시작하는 인수 인덱스입니다.
        /// </param>
        /// <param name="alignment">
        /// The alignment width represented by this segment.<br/>
        /// 이 세그먼트가 나타내는 정렬 너비입니다.
        /// </param>
        /// <param name="format">
        /// The custom format string represented by this segment.<br/>
        /// 이 세그먼트가 나타내는 사용자 지정 format 문자열입니다.
        /// </param>
        public void Deconstruct(out string? literal, out int index, out int alignment, out string? format)
        {
            literal = this.literal;

            index = this.index;
            alignment = this.alignment;
            format = this.format;
        }
    }
}
