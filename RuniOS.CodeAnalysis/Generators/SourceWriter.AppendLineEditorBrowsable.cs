using System;
using System.ComponentModel;

namespace RuniOS.CodeAnalysis.Generators;

public partial class SourceWriter
{
    public SourceWriter AppendLineEditorBrowsable(EditorBrowsableState state)
    {
        string value = state switch
        {
            EditorBrowsableState.Always => "Always",
            EditorBrowsableState.Never => "Never",
            EditorBrowsableState.Advanced => "Advanced",
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

        Append("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.");
        Append(value);
        AppendLine(")]");

        return this;
    }
}