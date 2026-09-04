using System;

namespace RuniOS.CodeAnalysis.Generators;

public partial class SourceWriter
{
    public IndentScope Indent() => new IndentScope(this);

    public readonly ref struct IndentScope : IDisposable
    {
        internal IndentScope(SourceWriter writer)
        {
            this.writer = writer;
            writer.indentLevel++;
        }

        readonly SourceWriter? writer;

        public void Dispose()
        {
            if (writer != null)
                writer.indentLevel--;
        }
    }
}