namespace RuniOS.CodeAnalysis.Generators;

/// <summary>
/// Builds generated source text one line at a time while tracking indentation.<br/>
/// 들여쓰기를 추적하면서 생성된 소스 텍스트를 한 줄씩 구성합니다.
/// </summary>
public sealed partial class SourceWriter
{
    public SourceWriter Append(bool value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(byte value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(char value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(char value, int repeatCount)
    {
        AppendIndentationIfNeeded();
        builder.Append(value, repeatCount);

        return this;
    }

    public SourceWriter Append(decimal value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(double value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(short value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(int value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(long value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(object? value)
    {
        if (value == null)
            return this;

        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(sbyte value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(float value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return this;

        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(string? value, int startIndex, int count)
    {
        if (string.IsNullOrEmpty(value))
            return this;

        AppendIndentationIfNeeded();
        builder.Append(value, startIndex, count);

        return this;
    }

    public SourceWriter Append(ushort value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(uint value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }

    public SourceWriter Append(ulong value)
    {
        AppendIndentationIfNeeded();
        builder.Append(value);

        return this;
    }
}