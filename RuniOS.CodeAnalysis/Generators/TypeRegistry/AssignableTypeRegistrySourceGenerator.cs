using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuniOS.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

[Generator]
public sealed class AssignableTypeRegistrySourceGenerator : TypeRegistrySourceGenerator
{
    const string registryMetadataName = "RuniOS.Reflection.AssignableTypeRegistry";
    const string immutableArrayMetadataName = "System.Collections.Immutable.ImmutableArray`1";
    const string systemTypeMetadataName = "System.Type";

    protected override string generatorName => "AssignableTypeRegistry";

    protected override string registryAttributeMetadataName => "RuniOS.Reflection.GenerateAssignableTypeRegistryAttribute";

    protected override bool TryGetRegistryBaseTypes
    (
        ImmutableArray<TypedConstant> constructorArguments,
        INamedTypeSymbol ownerType,
        bool isManifest,
        out ImmutableArray<ITypeSymbol> baseTypes
    )
    {
        int baseTypeArgumentIndex = isManifest ? 2 : 0;
        if (!isManifest && constructorArguments.Length > 0 && constructorArguments[0].Kind == TypedConstantKind.Primitive && constructorArguments[0].Value is bool)
            baseTypeArgumentIndex = 1;
        else if (isManifest && constructorArguments.Length > 2 && constructorArguments[2].Kind == TypedConstantKind.Primitive && constructorArguments[2].Value is bool)
            baseTypeArgumentIndex = 3;
        if (constructorArguments.Length <= baseTypeArgumentIndex)
        {
            baseTypes = ImmutableArray<ITypeSymbol>.Empty;
            return true;
        }

        TypedConstant firstArgument = constructorArguments[baseTypeArgumentIndex];
        ImmutableArray<TypedConstant> typeArguments = firstArgument.Kind == TypedConstantKind.Array
            ? firstArgument.Values
            : constructorArguments.Skip(baseTypeArgumentIndex).ToImmutableArray();
        ImmutableArray<ITypeSymbol>.Builder decodedTypes = ImmutableArray.CreateBuilder<ITypeSymbol>(typeArguments.Length);
        foreach (TypedConstant typeArgument in typeArguments)
        {
            if (typeArgument.Value is not ITypeSymbol baseType)
            {
                baseTypes = ImmutableArray<ITypeSymbol>.Empty;
                return false;
            }

            decodedTypes.Add(baseType);
        }

        baseTypes = decodedTypes.ToImmutable();
        return true;
    }

    protected override bool GetRequireDefaultConstructor(ImmutableArray<TypedConstant> constructorArguments, bool isManifest)
    {
        int index = isManifest ? 2 : 0;
        return constructorArguments.Length > index && constructorArguments[index].Kind == TypedConstantKind.Primitive && constructorArguments[index].Value is bool value && value;
    }

    protected override string CreateRegistryInitializer(RegistryDefinition registry) =>
        $"new {GeneratorUtils.GetTypeName(registry.registryType)}({(registry.requireDefaultConstructor ? "true" : "false")}, global::System.Collections.Immutable.ImmutableArray.Create<global::System.Type>({string.Join(", ", registry.baseTypes.Select(baseType => $"typeof({GeneratorUtils.GetTypeOfName(baseType)})"))}))";

    protected override bool HasAccessibleRegistryConstructor(INamedTypeSymbol registryType, Compilation compilation)
    {
        INamedTypeSymbol? systemType = compilation.GetTypeByMetadataName(systemTypeMetadataName);
        INamedTypeSymbol? immutableArray = compilation.GetTypeByMetadataName(immutableArrayMetadataName);
        if (systemType == null || immutableArray == null)
            return false;

        INamedTypeSymbol expectedParameterType = immutableArray.Construct(systemType);
        foreach (IMethodSymbol constructor in registryType.InstanceConstructors)
        {
            if (constructor.Parameters.Length != 1)
                continue;

            IParameterSymbol parameter = constructor.Parameters[0];
            if (!parameter.IsParams || !SymbolEqualityComparer.Default.Equals(parameter.Type, expectedParameterType))
                continue;

            if (TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode(constructor, compilation))
                return true;
        }

        return false;
    }

    protected override bool IsSupportedRegistryType(INamedTypeSymbol registryType, Compilation compilation)
    {
        INamedTypeSymbol? registrySymbol = compilation.GetTypeByMetadataName(registryMetadataName);
        return registrySymbol != null && SymbolEqualityComparer.Default.Equals(registryType.OriginalDefinition, registrySymbol);
    }

    protected override IncrementalValuesProvider<RegistrationCandidate> CreateCandidateProvider(IncrementalGeneratorInitializationContext context) =>
        context.SyntaxProvider.CreateSyntaxProvider
        (
            static (node, _) => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax,
            static (syntaxContext, cancellationToken) => CreateCandidate(syntaxContext, cancellationToken)
        )
        .Where(static candidate => candidate != null)
        .Select(static RegistrationCandidate (candidate, _) => candidate!);

    protected override bool TryBindCandidate
    (
        RegistryDefinition registry,
        RegistrationCandidate candidate,
        Compilation compilation,
        out BoundRegistration? registration,
        out Diagnostic? diagnostic
    )
    {
        registration = null;
        diagnostic = null;

        if (candidate is not AssignableRegistrationCandidate assignableCandidate)
            return false;

        INamedTypeSymbol implementationType = assignableCandidate.implementationType;
        if (registry.baseTypes.Any(baseType => !TypeRegistrySymbolHelpers.IsSameOrDerived(implementationType, baseType)))
            return false;

        if (registry.requireDefaultConstructor && !HasPublicParameterlessConstructor(implementationType))
            return false;

        if (!TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode(implementationType, compilation))
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.inaccessibleAttribute,
                TypeRegistrySymbolHelpers.GetLocation(implementationType),
                compilation,
                implementationType,
                implementationType
            );
            return false;
        }

        if
        (
            !HasReferencedLifecycleApi(compilation, onAssemblyLoadedAttributeMetadataName) ||
            !HasReferencedLifecycleApi(compilation, onAssemblyUnloadingAttributeMetadataName) ||
            !HasReferencedLifecycleApi(compilation, lifecycleMethodRegistrationMetadataName)
        )
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.missingLifecycleApi,
                TypeRegistrySymbolHelpers.GetLocation(implementationType),
                compilation
            );
            return false;
        }

        registration = new BoundRegistration(registry, implementationType, null);
        return true;
    }

    static bool HasPublicParameterlessConstructor(INamedTypeSymbol type) =>
        type.InstanceConstructors.Any(constructor => constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public);

    protected override void EmitRegisterStatements(SourceWriter writer, RegistryDefinition registry, ImmutableArray<BoundRegistration> registrations)
    {
        writer.AppendLine($"{GetRegistryAccess(registry)}.RegisterRangeUnchecked(");
        writer.Indent();

        for (int index = 0; index < registrations.Length; index++)
        {
            string implementationType = GeneratorUtils.GetTypeOfGenericDefinitionName(registrations[index].implementationType);
            writer.AppendLine($"typeof({implementationType}){(index + 1 < registrations.Length ? "," : string.Empty)}");
        }

        writer.Unindent();
        writer.AppendLine(");");
    }

    static AssignableRegistrationCandidate? CreateCandidate(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol implementationType)
            return null;

        return new AssignableRegistrationCandidate(implementationType);
    }
}
