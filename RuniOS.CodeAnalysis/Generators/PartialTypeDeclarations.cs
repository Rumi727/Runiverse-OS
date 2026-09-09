using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Generators;

public readonly record struct PartialTypeDeclarations(string? nameSpace, ImmutableArray<PartialTypeDeclaration> declarations);