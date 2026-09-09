using System;

namespace RuniOS.CodeAnalysis.Generators;

public partial class SourceWriter
{
    public BlockScope Block() => new BlockScope(this);

    public void BeginBlock()
    {
        if (!isLineStart)
            AppendLine();

        AppendLine("{");
        BeginIndent();
    }

    public void EndBlock()
    {
        EndIndent();

        if (!isLineStart)
            AppendLine();

        AppendLine("}");
    }

    public readonly ref struct BlockScope : IDisposable
    {
        internal BlockScope(SourceWriter writer)
        {
            this.writer = writer;
            writer.BeginBlock();
        }

        readonly SourceWriter? writer;

        public void Dispose() => writer?.EndBlock();
    }
}