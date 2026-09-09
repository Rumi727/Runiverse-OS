using Microsoft.CodeAnalysis;
using System;

namespace RuniOS.CodeAnalysis.Generators;

public static class PartialTypeDeclarationsSerializer
{
    public static Scope Serialize(SourceWriter writer, PartialTypeDeclarations declarations, out SerializeErrorResults errors)
    {
        errors = default;
        if (declarations.nameSpace != null)
        {
            if (!writer.BeginNamespace(declarations.nameSpace))
                errors |= new SerializeErrorResults(SerializeError.invalidIdentifier, declarations.nameSpace);
        }

        for (int i = 0; i < declarations.declarations.Length; i++)
        {
            PartialTypeDeclaration declaration = declarations.declarations[i];
            errors |= RenderTypeDeclaration(writer, declaration);

            writer.BeginBlock();
        }

        return new Scope(writer, declarations.nameSpace == null, declarations.declarations.Length);
    }

    public readonly ref struct Scope : IDisposable
    {
        internal Scope(SourceWriter writer, bool isGlobalNamespace, int declarationCount)
        {
            this.writer = writer;
            this.isGlobalNamespace = isGlobalNamespace;
            this.declarationCount = declarationCount;
        }

        readonly SourceWriter? writer;
        readonly bool isGlobalNamespace;
        readonly int declarationCount;

        public void Dispose()
        {
            if (writer == null)
                return;

            for (int i = 0; i < declarationCount; i++)
                writer.EndBlock();

            if (!isGlobalNamespace)
                writer.EndNamespace();
        }
    }

    static SerializeErrorResults RenderTypeDeclaration(SourceWriter writer, PartialTypeDeclaration declaration)
    {
        SerializeErrorResults result = default;

        writer.Append("partial ");
        if (declaration.isRecord)
            writer.Append("record");

        switch (declaration.typeKind)
        {
            case TypeKind.Class:
            {
                writer.Append("class ");
                break;
            }
            case TypeKind.Struct:
            {
                writer.Append("struct ");
                break;
            }
            case TypeKind.Interface:
            {
                if (declaration.isRecord)
                    result |= new SerializeErrorResults(SerializeError.unrepresentableType, declaration);

                writer.Append("interface ");
                break;
            }
            default:
            {
                result |= new SerializeErrorResults(SerializeError.unrepresentableType, declaration);
                break;
            }
        }

        result |= RenderIdentifier(writer, declaration.name);

        if (declaration.typeParameters.IsDefaultOrEmpty)
            return result;

        writer.Append('<');

        for (int i = 0; i < declaration.typeParameters.Length; i++)
        {
            result |= RenderIdentifier(writer, declaration.typeParameters[i]);
            if (i < declaration.typeParameters.Length - 1)
                writer.Append(", ");
        }

        writer.Append('>');
        return result;
    }

    static SerializeErrorResults RenderIdentifier(SourceWriter writer, string identifier)
    {
        if (!writer.AppendIdentifier(identifier))
            return new SerializeErrorResults(SerializeError.invalidIdentifier, identifier);

        return default;
    }
}