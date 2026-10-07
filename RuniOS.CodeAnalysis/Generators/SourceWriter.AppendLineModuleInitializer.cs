namespace RuniOS.CodeAnalysis.Generators;

public partial class SourceWriter
{
    public SourceWriter AppendLineModuleInitializer()
    {
        AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
        return this;
    }
}