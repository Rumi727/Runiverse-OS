namespace RuniOS.CodeAnalysis.Generators;

/// <summary>
/// Builds generated source text one line at a time while tracking indentation.<br/>
/// 들여쓰기를 추적하면서 생성된 소스 텍스트를 한 줄씩 구성합니다.
/// </summary>
public sealed partial class SourceWriter
{
    public SourceWriter AppendLine()
    {
        builder.AppendLine();
        return this;
    }

    /// <summary>
    /// Appends a line using the current indentation depth.<br/>
    /// 현재 들여쓰기 깊이를 적용한 줄을 추가합니다.
    /// </summary>
    /// <param name="value">
    /// The text to append; an empty value appends only a line terminator.<br/>
    /// 추가할 텍스트이며, 빈 값이면 줄바꿈만 추가합니다.
    /// </param>
    public SourceWriter AppendLine(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            builder.AppendLine();
            return this;
        }

        AppendIndentationIfNeeded();
        builder.AppendLine(value);

        return this;
    }
}