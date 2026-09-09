using System;

namespace RuniOS.CodeAnalysis.Generators;

public partial class SourceWriter
{
    public NamespaceScope Namespace(string? nameSpace) => new NamespaceScope(this, nameSpace);

    public bool BeginNamespace(string nameSpace)
    {
        EnsureBlankLine();

        Append("namespace ");
        bool valid = AppendIdentifier(nameSpace);

        BeginBlock();
        return valid;
    }

    public void EndNamespace() => EndBlock();

    public readonly ref struct NamespaceScope : IDisposable
    {
        internal NamespaceScope(SourceWriter writer, string? nameSpace)
        {
            if (string.IsNullOrEmpty(nameSpace))
                return;

            this.writer = writer;
            writer.BeginNamespace(nameSpace!);
        }

        readonly SourceWriter? writer;

        public void Dispose() => writer?.EndNamespace();
    }
}