using Microsoft.CodeAnalysis.CSharp;

namespace RuniOS.CodeAnalysis.Generators;

public sealed partial class SourceWriter
{
    public bool AppendIdentifier(string identifier)
    {
        if (!SyntaxFacts.IsValidIdentifier(identifier))
            return false;

        if (SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None)
            Append('@');

        Append(identifier);
        return true;
    }
}