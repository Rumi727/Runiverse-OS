using System;

namespace RuniOS.CodeAnalysis.Generators;

public partial class SourceWriter
{
    public IndentScope Indent() => new IndentScope(this);

    public void BeginIndent() => indentLevel++;
    public void EndIndent() => indentLevel--;

    public readonly ref struct IndentScope : IDisposable
    {
        internal IndentScope(SourceWriter writer)
        {
            this.writer = writer;
            writer.BeginIndent();
        }

        readonly SourceWriter? writer;

        public void Dispose() => writer?.EndIndent();
    }
}