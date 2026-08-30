using Microsoft.CodeAnalysis;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

sealed class AssignableRegistrationCandidate(INamedTypeSymbol implementationType) : RegistrationCandidate(implementationType)
{
    public INamedTypeSymbol implementationType { get; } = implementationType;
}
