namespace RuniOS.CodeAnalysis.Generators;

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