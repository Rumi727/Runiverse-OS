using System;

namespace RuniOS.CodeAnalysis.Generators;

public partial class SourceWriter
{
    public BlockScope Block() => new BlockScope(this);

    public readonly ref struct BlockScope : IDisposable
    {
        internal BlockScope(SourceWriter writer)
        {
            this.writer = writer;

            if (!writer.isLineStart)
                writer.AppendLine();

            writer.AppendLine("{");
            writer.indentLevel++;
        }

        readonly SourceWriter? writer;

        public void Dispose()
        {
            if (writer != null)
            {
                writer.indentLevel--;

                if (!writer.isLineStart)
                    writer.AppendLine();

                writer.AppendLine("}");
            }
        }
    }
}