using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Generators;

public readonly record struct PartialTypeDeclaration(TypeKind typeKind, bool isRecord, string name, ImmutableArray<string> typeParameters)
{
    public string name { get => field ?? string.Empty; init; } = name;
}