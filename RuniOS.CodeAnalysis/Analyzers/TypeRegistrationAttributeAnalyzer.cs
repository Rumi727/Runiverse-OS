using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RuniOS.CodeAnalysis.Diagnostics;
using RuniOS.CodeAnalysis.Generators;
using RuniOS.CodeAnalysis.Generators.TypeRegistry;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RuniOS.CodeAnalysis.Analyzers;

/// <summary>
/// Warns when a type-registration attribute has no generated attributed registry in its base type hierarchy.<br/>
/// 기본 타입 계층에 생성된 특성 기반 레지스트리가 없는 타입 등록 특성을 경고합니다.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeRegistrationAttributeAnalyzer : DiagnosticAnalyzer
{
    const string typeRegistrationAttributeMetadataName = "RuniOS.Reflection.TypeRegistrationAttribute";
    const string attributedTypeRegistryMetadataName = "RuniOS.Reflection.AttributedTypeRegistry`2";
    const string generateTypeRegistryAttributeMetadataName = "RuniOS.Reflection.GenerateTypeRegistryAttribute";
    const string typeRegistryManifestAttributeMetadataName = "RuniOS.Reflection.TypeRegistryManifestAttribute";

    /// <summary>
    /// Gets the diagnostic reported for registrations without a matching registry.<br/>
    /// 일치하는 레지스트리 없는 등록에 대해 보고하는 진단 컬렉션을 가져옵니다.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(TypeRegistryDiagnostics.registrationWithoutRegistry);

    /// <summary>
    /// Registers analysis for types carrying attributes derived from <c>TypeRegistrationAttribute</c>.<br/>
    /// <c>TypeRegistrationAttribute</c>에서 파생된 특성을 가진 타입 분석을 등록합니다.
    /// </summary>
    /// <param name="context">
    /// The analyzer initialization context.<br/>
    /// 분석기 초기화 컨텍스트입니다.
    /// </param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static context =>
        {
            INamedTypeSymbol? registrationAttribute = context.Compilation.GetTypeByMetadataName(typeRegistrationAttributeMetadataName);
            INamedTypeSymbol? attributedRegistry = context.Compilation.GetTypeByMetadataName(attributedTypeRegistryMetadataName);
            if (registrationAttribute == null || attributedRegistry == null)
                return;

            ImmutableArray<IPropertySymbol> referencedRegistryProperties = FindReferencedRegistryProperties(context.Compilation);
            context.RegisterSymbolAction
            (
                symbolContext => AnalyzeType
                (
                    symbolContext,
                    registrationAttribute,
                    attributedRegistry,
                    referencedRegistryProperties
                ),
                SymbolKind.NamedType
            );
        });
    }

    static void AnalyzeType
    (
        SymbolAnalysisContext context,
        INamedTypeSymbol registrationAttribute,
        INamedTypeSymbol attributedRegistry,
        ImmutableArray<IPropertySymbol> referencedRegistryProperties
    )
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } implementationType)
            return;

        foreach (AttributeData attribute in implementationType.GetAttributes())
        {
            if (attribute.AttributeClass is not INamedTypeSymbol attributeType || !TypeRegistrySymbolHelpers.IsSameOrDerived(attributeType, registrationAttribute))
                continue;

            if (HasMatchingRegistry(implementationType, attributeType, attributedRegistry, referencedRegistryProperties))
                continue;

            context.ReportDiagnostic
            (
                TypeRegistryDiagnostics.Create
                (
                    TypeRegistryDiagnostics.registrationWithoutRegistry,
                    TypeRegistrySymbolHelpers.GetLocation(attribute),
                    attributeType.Name,
                    implementationType.Name
                )
            );
        }
    }

    static bool HasMatchingRegistry
    (
        INamedTypeSymbol implementationType,
        INamedTypeSymbol registrationAttribute,
        INamedTypeSymbol attributedRegistry,
        ImmutableArray<IPropertySymbol> referencedRegistryProperties
    )
    {
        for (INamedTypeSymbol? current = implementationType; current != null; current = current.BaseType)
        {
            foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!IsCurrentGeneratedRegistryProperty(property, attributedRegistry) || !MatchesRegistry(property, implementationType, registrationAttribute))
                    continue;

                return true;
            }
        }

        foreach (IPropertySymbol property in referencedRegistryProperties)
        {
            if (!IsBaseTypeOrSelf(implementationType, property.ContainingType) || !MatchesRegistry(property, implementationType, registrationAttribute))
                continue;

            return true;
        }

        return false;
    }

    static bool IsCurrentGeneratedRegistryProperty(IPropertySymbol property, INamedTypeSymbol attributedRegistry)
    {
        if (!IsRegistryPropertyShape(property, attributedRegistry) || !HasPartialPropertyDefinition(property))
            return false;
        if (property.ContainingType.TypeKind != TypeKind.Class && property.ContainingType.TypeKind != TypeKind.Struct)
            return false;
        if (!TypeRegistrySymbolHelpers.IsPartialTypeHierarchy(property.ContainingType))
            return false;

        for (INamedTypeSymbol? current = property.ContainingType; current != null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
                return false;
        }

        return property.GetAttributes().Any(attribute =>
            attribute.AttributeClass is INamedTypeSymbol attributeType &&
            GeneratorUtils.GetMetadataName(attributeType) == generateTypeRegistryAttributeMetadataName);
    }

    static bool IsRegistryPropertyShape(IPropertySymbol property, INamedTypeSymbol attributedRegistry)
    {
        return !property.IsIndexer &&
            property.IsStatic &&
            property.DeclaredAccessibility == Accessibility.Public &&
            property.GetMethod is { DeclaredAccessibility: Accessibility.Public } &&
            property.SetMethod == null &&
            property.Type is INamedTypeSymbol registryType &&
            SymbolEqualityComparer.Default.Equals(registryType.OriginalDefinition, attributedRegistry);
    }

    static bool HasPartialPropertyDefinition(IPropertySymbol property)
    {
        foreach (SyntaxReference reference in property.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is PropertyDeclarationSyntax declaration && TypeRegistrySymbolHelpers.IsPartialPropertyDefinition(declaration))
                return true;
        }

        return false;
    }

    static bool MatchesRegistry(IPropertySymbol property, INamedTypeSymbol implementationType, INamedTypeSymbol registrationAttribute)
    {
        if (property.Type is not INamedTypeSymbol registryType || registryType.TypeArguments.Length != 2)
            return false;

        return TypeRegistrySymbolHelpers.IsSameOrDerived(implementationType, registryType.TypeArguments[0]) &&
            TypeRegistrySymbolHelpers.IsSameOrDerived(registrationAttribute, registryType.TypeArguments[1]);
    }

    static bool IsBaseTypeOrSelf(INamedTypeSymbol implementationType, INamedTypeSymbol possibleBaseType)
    {
        for (INamedTypeSymbol? current = implementationType; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, possibleBaseType))
                return true;
        }

        return false;
    }

    static ImmutableArray<IPropertySymbol> FindReferencedRegistryProperties(Compilation compilation)
    {
        ImmutableArray<IPropertySymbol>.Builder properties = ImmutableArray.CreateBuilder<IPropertySymbol>();
        HashSet<IPropertySymbol> seenProperties = new(SymbolEqualityComparer.Default);

        foreach (IAssemblySymbol assembly in EnumerateReferencedAssemblies(compilation))
        {
            foreach (AttributeData manifest in assembly.GetAttributes())
            {
                if (manifest.AttributeClass is not INamedTypeSymbol manifestType || GeneratorUtils.GetMetadataName(manifestType) != typeRegistryManifestAttributeMetadataName)
                    continue;
                if (manifest.ConstructorArguments.Length < 2 ||
                    manifest.ConstructorArguments[0].Value is not INamedTypeSymbol ownerType ||
                    manifest.ConstructorArguments[1].Value is not string propertyName)
                    continue;

                IPropertySymbol? property = ownerType.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault();
                if (property != null && seenProperties.Add(property))
                    properties.Add(property);
            }
        }

        return properties.ToImmutable();
    }

    static IEnumerable<IAssemblySymbol> EnumerateReferencedAssemblies(Compilation compilation)
    {
        HashSet<IAssemblySymbol> seen = new(SymbolEqualityComparer.Default);
        Stack<IAssemblySymbol> pending = new();
        foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols)
            pending.Push(assembly);

        while (pending.Count != 0)
        {
            IAssemblySymbol assembly = pending.Pop();
            if (!seen.Add(assembly))
                continue;

            yield return assembly;
            foreach (IModuleSymbol module in assembly.Modules)
            {
                foreach (IAssemblySymbol referencedAssembly in module.ReferencedAssemblySymbols)
                    pending.Push(referencedAssembly);
            }
        }
    }
}
