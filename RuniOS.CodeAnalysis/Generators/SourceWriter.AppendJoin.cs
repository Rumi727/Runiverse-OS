namespace RuniOS.CodeAnalysis.Generators;

/// <summary>
/// Builds generated source text one line at a time while tracking indentation.<br/>
/// 들여쓰기를 추적하면서 생성된 소스 텍스트를 한 줄씩 구성합니다.
/// </summary>
public sealed partial class SourceWriter
{
    public SourceWriter AppendJoin(char separator, params object?[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            Append(values[i]);
            if (i < values.Length - 1)
                Append(separator);
        }
        return this;
    }

    public SourceWriter AppendJoin(char separator, params string?[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            Append(values[i]);
            if (i < values.Length - 1)
                Append(separator);
        }
        return this;
    }

    public SourceWriter AppendJoin(string separator, params object?[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            Append(values[i]);
            if (i < values.Length - 1)
                Append(separator);
        }
        return this;
    }

    public SourceWriter AppendJoin(string separator, params string?[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            Append(values[i]);
            if (i < values.Length - 1)
                Append(separator);
        }
        return this;
    }
}