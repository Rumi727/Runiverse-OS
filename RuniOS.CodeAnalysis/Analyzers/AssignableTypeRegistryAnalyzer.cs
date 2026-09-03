using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RuniOS.CodeAnalysis.Diagnostics;
using RuniOS.CodeAnalysis.Generators.TypeRegistry;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RuniOS.CodeAnalysis.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AssignableTypeRegistryAnalyzer : DiagnosticAnalyzer
{
    const string registryMetadataName = "RuniOS.Reflection.AssignableTypeRegistry";
    const string markerMetadataName = "RuniOS.Reflection.GenerateAssignableTypeRegistryAttribute";
    const string manifestMetadataName = "RuniOS.Reflection.TypeRegistryManifestAttribute";

    readonly record struct RegistryConfiguration(ImmutableArray<ITypeSymbol> baseTypes);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(TypeRegistryDiagnostics.assignableRegistrationRequiresDefaultConstructor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static context =>
        {
            INamedTypeSymbol? registryType = context.Compilation.GetTypeByMetadataName(registryMetadataName);
            INamedTypeSymbol? markerType = context.Compilation.GetTypeByMetadataName(markerMetadataName);
            INamedTypeSymbol? manifestType = context.Compilation.GetTypeByMetadataName(manifestMetadataName);
            if (registryType == null || markerType == null || manifestType == null)
                return;

            ImmutableArray<RegistryConfiguration> configurations = FindConfigurations(context.Compilation, registryType, markerType, manifestType);
            if (configurations.IsEmpty)
                return;

            context.RegisterSymbolAction
            (
                symbolContext => AnalyzeType(symbolContext, configurations),
                SymbolKind.NamedType
            );
        });
    }

    static void AnalyzeType(SymbolAnalysisContext context, ImmutableArray<RegistryConfiguration> configurations)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } implementationType || HasPublicParameterlessConstructor(implementationType))
            return;

        if (!configurations.Any(configuration => configuration.baseTypes.All(baseType => TypeRegistrySymbolHelpers.IsSameOrDerived(implementationType, baseType))))
            return;

        context.ReportDiagnostic
        (
            TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.assignableRegistrationRequiresDefaultConstructor,
                TypeRegistrySymbolHelpers.GetLocation(implementationType),
                context.Compilation,
                implementationType
            )
        );
    }

    static bool HasPublicParameterlessConstructor(INamedTypeSymbol type) =>
        type.InstanceConstructors.Any(constructor => constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public);

    static ImmutableArray<RegistryConfiguration> FindConfigurations
    (
        Compilation compilation,
        INamedTypeSymbol registryType,
        INamedTypeSymbol markerType,
        INamedTypeSymbol manifestType
    )
    {
        ImmutableArray<RegistryConfiguration>.Builder result = ImmutableArray.CreateBuilder<RegistryConfiguration>();
        foreach (INamedTypeSymbol type in EnumerateTypes(compilation.GlobalNamespace))
        {
            foreach (IPropertySymbol property in type.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.Type is not INamedTypeSymbol propertyType || !SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, registryType))
                    continue;

                AttributeData? marker = property.GetAttributes().FirstOrDefault(attribute =>
                    attribute.AttributeClass != null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, markerType));
                if (marker != null && TryDecode(marker.ConstructorArguments, isManifest: false, out ImmutableArray<ITypeSymbol> baseTypes, out bool requireDefaultConstructor) && requireDefaultConstructor)
                    result.Add(new RegistryConfiguration(baseTypes));
            }
        }

        foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            foreach (AttributeData manifest in assembly.GetAttributes())
            {
                if (manifest.AttributeClass == null || !SymbolEqualityComparer.Default.Equals(manifest.AttributeClass, manifestType))
                    continue;
                if (TryDecode(manifest.ConstructorArguments, isManifest: true, out ImmutableArray<ITypeSymbol> baseTypes, out bool requireDefaultConstructor) && requireDefaultConstructor)
                    result.Add(new RegistryConfiguration(baseTypes));
            }
        }

        return result.ToImmutable();
    }

    static bool TryDecode
    (
        ImmutableArray<TypedConstant> arguments,
        bool isManifest,
        out ImmutableArray<ITypeSymbol> baseTypes,
        out bool requireDefaultConstructor
    )
    {
        int baseTypeIndex = isManifest ? 2 : 0;
        requireDefaultConstructor = false;
        if (isManifest && arguments.Length > 2 && arguments[2].Kind == TypedConstantKind.Primitive && arguments[2].Value is bool manifestValue)
        {
            requireDefaultConstructor = manifestValue;
            baseTypeIndex = 3;
        }
        else if (!isManifest && arguments.Length > 0 && arguments[0].Kind == TypedConstantKind.Primitive && arguments[0].Value is bool markerValue)
        {
            requireDefaultConstructor = markerValue;
            baseTypeIndex = 1;
        }

        ImmutableArray<TypedConstant> typeArguments = arguments.Length <= baseTypeIndex
            ? ImmutableArray<TypedConstant>.Empty
            : arguments[baseTypeIndex].Kind == TypedConstantKind.Array
                ? arguments[baseTypeIndex].Values
                : arguments.Skip(baseTypeIndex).ToImmutableArray();
        ImmutableArray<ITypeSymbol>.Builder decoded = ImmutableArray.CreateBuilder<ITypeSymbol>(typeArguments.Length);
        foreach (TypedConstant argument in typeArguments)
        {
            if (argument.Value is not ITypeSymbol type)
            {
                baseTypes = ImmutableArray<ITypeSymbol>.Empty;
                return false;
            }
            decoded.Add(type);
        }

        baseTypes = decoded.ToImmutable();
        return true;
    }

    static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol space)
    {
        foreach (INamespaceSymbol child in space.GetNamespaceMembers())
            foreach (INamedTypeSymbol type in EnumerateTypes(child))
                yield return type;
        foreach (INamedTypeSymbol type in space.GetTypeMembers())
            foreach (INamedTypeSymbol nested in EnumerateTypes(type))
                yield return nested;
    }

    static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamedTypeSymbol type)
    {
        yield return type;
        foreach (INamedTypeSymbol nestedType in type.GetTypeMembers())
            foreach (INamedTypeSymbol nested in EnumerateTypes(nestedType))
                yield return nested;
    }
}
