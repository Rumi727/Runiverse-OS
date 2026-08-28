using System.Text;

namespace RuniOS.CodeAnalysis.Generators;

/// <summary>
/// Builds generated source text one line at a time while tracking indentation.<br/>
/// 들여쓰기를 추적하면서 생성된 소스 텍스트를 한 줄씩 구성합니다.
/// </summary>
public sealed class SourceWriter
{
    readonly StringBuilder builder = new();
    int indentation;

    /// <summary>
    /// Appends a line using the current indentation depth.<br/>
    /// 현재 들여쓰기 깊이를 적용한 줄을 추가합니다.
    /// </summary>
    /// <param name="value">
    /// The text to append; an empty value appends only a line terminator.<br/>
    /// 추가할 텍스트이며, 빈 값이면 줄바꿈만 추가합니다.
    /// </param>
    public void AppendLine(string value = "")
    {
        if (value.Length != 0)
            builder.Append(' ', indentation * 4).Append(value);

        builder.AppendLine();
    }

    /// <summary>
    /// Increases the indentation depth by one level.<br/>
    /// 들여쓰기 깊이를 한 단계 증가시킵니다.
    /// </summary>
    public void Indent() => indentation++;

    /// <summary>
    /// Decreases the indentation depth by one level.<br/>
    /// 들여쓰기 깊이를 한 단계 감소시킵니다.
    /// </summary>
    public void Unindent() => indentation--;

    /// <summary>
    /// Returns all source text appended to this writer.<br/>
    /// 이 작성기에 추가된 전체 소스 텍스트를 반환합니다.
    /// </summary>
    /// <returns>
    /// The accumulated source text.<br/>
    /// 누적된 소스 텍스트입니다.
    /// </returns>
    public override string ToString() => builder.ToString();
}
