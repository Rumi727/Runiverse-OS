namespace RuniOS.CodeAnalysis.Generators;

public partial class SourceWriter
{
    public SourceWriter AppendLineCompilerGenerated()
    {
        AppendLine("[global::System.Runtime.CompilerServices.CompilerGenerated]");
        return this;
    }
}