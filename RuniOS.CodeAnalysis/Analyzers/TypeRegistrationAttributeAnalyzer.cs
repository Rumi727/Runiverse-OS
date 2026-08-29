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
/// Warns when a type-registration attribute has no generated attributed registry, cannot resolve a generic registration, or has mismatched generic constraints.<br/>
/// 생성된 특성 기반 레지스트리가 없거나 제네릭 등록을 확인할 수 없거나 제네릭 제약 조건이 일치하지 않는 타입 등록 특성을 경고합니다.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeRegistrationAttributeAnalyzer : DiagnosticAnalyzer
{
    const string typeRegistrationAttributeMetadataName = "RuniOS.Reflection.TypeRegistrationAttribute";
    const string attributedTypeRegistryMetadataName = "RuniOS.Reflection.AttributedTypeRegistry`1";
    const string generateTypeRegistryAttributeMetadataName = "RuniOS.Reflection.GenerateTypeRegistryAttribute";
    const string typeRegistryManifestAttributeMetadataName = "RuniOS.Reflection.TypeRegistryManifestAttribute";

    readonly record struct ReferencedRegistryProperty(IPropertySymbol property, ITypeSymbol baseType);

    /// <summary>
    /// Gets the diagnostics reported for registrations without a matching registry, with an unresolvable generic target, or with mismatched generic constraints.<br/>
    /// 일치하는 레지스트리가 없거나 확인할 수 없는 제네릭 대상 또는 일치하지 않는 제네릭 제약 조건을 가진 등록에 대해 보고하는 진단 컬렉션을 가져옵니다.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create
        (
            TypeRegistryDiagnostics.registrationWithoutRegistry,
            TypeRegistryDiagnostics.openGenericRegistrationRequiresChildren,
            TypeRegistryDiagnostics.genericRegistrationParameterCountMismatch,
            TypeRegistryDiagnostics.genericRegistrationConstraintMismatch,
            TypeRegistryDiagnostics.genericRegistrationSuggestion
        );

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

            ImmutableArray<ReferencedRegistryProperty> referencedRegistryProperties = FindReferencedRegistryProperties(context.Compilation);
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
        ImmutableArray<ReferencedRegistryProperty> referencedRegistryProperties
    )
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } implementationType)
            return;

        foreach (AttributeData attribute in implementationType.GetAttributes())
        {
            if (attribute.AttributeClass == null || !TypeRegistrySymbolHelpers.IsSameOrDerived(attribute.AttributeClass, registrationAttribute))
                continue;

            if (!HasMatchingRegistry(implementationType, attribute.AttributeClass, attributedRegistry, referencedRegistryProperties))
            {
                context.ReportDiagnostic
                (
                    TypeRegistryDiagnostics.Create
                    (
                        TypeRegistryDiagnostics.registrationWithoutRegistry,
                        TypeRegistrySymbolHelpers.GetLocation(attribute),
                        context.Compilation,
                        attribute.AttributeClass.Name,
                        implementationType.Name
                    )
                );
                continue;
            }

            AnalyzeGenericRegistration(context, attribute, attribute.AttributeClass, implementationType);
        }
    }

    static void AnalyzeGenericRegistration
    (
        SymbolAnalysisContext context,
        AttributeData attribute,
        INamedTypeSymbol attributeType,
        INamedTypeSymbol implementationType
    )
    {
        if (attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol targetType)
            return;

        if (!IsOpenGenericType(targetType))
        {
            if (targetType.IsGenericType || !implementationType.IsGenericType)
                return;

            ImmutableArray<ITypeParameterSymbol> implementationParameters = GetRuntimeTypeParameters(implementationType);
            if (implementationParameters.Length == 0)
                return;

            context.ReportDiagnostic
            (
                TypeRegistryDiagnostics.Create
                (
                    TypeRegistryDiagnostics.genericRegistrationParameterCountMismatch,
                    TypeRegistrySymbolHelpers.GetLocation(attribute),
                    context.Compilation,
                    attributeType.Name,
                    implementationType.Name,
                    targetType,
                    0,
                    implementationParameters.Length
                )
            );
            return;
        }

        if (!GetUseForChildren(attribute))
        {
            context.ReportDiagnostic
            (
                TypeRegistryDiagnostics.Create
                (
                    TypeRegistryDiagnostics.openGenericRegistrationRequiresChildren,
                    TypeRegistrySymbolHelpers.GetLocation(attribute),
                    context.Compilation,
                    attributeType.Name,
                    implementationType.Name,
                    targetType
                )
            );
            return;
        }

        if (!implementationType.IsGenericType)
        {
            context.ReportDiagnostic
            (
                TypeRegistryDiagnostics.Create
                (
                    TypeRegistryDiagnostics.genericRegistrationSuggestion,
                    TypeRegistrySymbolHelpers.GetLocation(attribute),
                    context.Compilation,
                    attributeType.Name,
                    implementationType.Name,
                    targetType
                )
            );
            return;
        }

        {
            ImmutableArray<ITypeParameterSymbol> targetParameters = GetRuntimeTypeParameters(targetType.OriginalDefinition);
            ImmutableArray<ITypeParameterSymbol> implementationParameters = GetRuntimeTypeParameters(implementationType);
            if (targetParameters.Length != implementationParameters.Length)
            {
                context.ReportDiagnostic
                (
                    TypeRegistryDiagnostics.Create
                    (
                        TypeRegistryDiagnostics.genericRegistrationParameterCountMismatch,
                        TypeRegistrySymbolHelpers.GetLocation(attribute),
                        context.Compilation,
                        attributeType.Name,
                        implementationType.Name,
                        targetType,
                        targetParameters.Length,
                        implementationParameters.Length
                    )
                );
                return;
            }

            if (!HasGenericConstraintMismatch(targetParameters, implementationParameters))
                return;

            context.ReportDiagnostic
            (
                TypeRegistryDiagnostics.Create
                (
                    TypeRegistryDiagnostics.genericRegistrationConstraintMismatch,
                    TypeRegistrySymbolHelpers.GetLocation(attribute),
                    context.Compilation,
                    attributeType.Name,
                    implementationType.Name,
                    targetType
                )
            );
        }
    }

    static bool IsOpenGenericType(INamedTypeSymbol type)
    {
        return type.IsGenericType &&
            (type.IsUnboundGenericType || type.TypeArguments.Any(ContainsTypeParameter));
    }

    static bool ContainsTypeParameter(ITypeSymbol type)
    {
        return type switch
        {
            ITypeParameterSymbol => true,
            INamedTypeSymbol namedType => namedType.TypeArguments.Any(ContainsTypeParameter),
            IArrayTypeSymbol arrayType => ContainsTypeParameter(arrayType.ElementType),
            IPointerTypeSymbol pointerType => ContainsTypeParameter(pointerType.PointedAtType),
            _ => false
        };
    }

    static bool GetUseForChildren(AttributeData attribute)
    {
        foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
        {
            if (namedArgument.Key == "useForChildren" && namedArgument.Value.Value is bool useForChildren)
                return useForChildren;
        }

        if (attribute.AttributeConstructor is not { } constructor)
            return false;

        for (int index = 0; index < attribute.ConstructorArguments.Length && index < constructor.Parameters.Length; index++)
        {
            IParameterSymbol parameter = constructor.Parameters[index];
            if (parameter.Name == "useForChildren" && parameter.Type.SpecialType == SpecialType.System_Boolean && attribute.ConstructorArguments[index].Value is bool useForChildren)
                return useForChildren;
        }

        return false;
    }

    static ImmutableArray<ITypeParameterSymbol> GetRuntimeTypeParameters(INamedTypeSymbol type)
    {
        Stack<INamedTypeSymbol> containingTypes = new();
        for (INamedTypeSymbol? current = type; current != null; current = current.ContainingType)
            containingTypes.Push(current);

        ImmutableArray<ITypeParameterSymbol>.Builder parameters = ImmutableArray.CreateBuilder<ITypeParameterSymbol>();
        while (containingTypes.Count != 0)
            parameters.AddRange(containingTypes.Pop().TypeParameters);

        return parameters.ToImmutable();
    }

    static bool HasUnsatisfiedGenericConstraints
    (
        ImmutableArray<ITypeParameterSymbol> targetParameters,
        ImmutableArray<ITypeParameterSymbol> implementationParameters
    )
    {
        for (int index = 0; index < implementationParameters.Length; index++)
        {
            ITypeParameterSymbol targetParameter = targetParameters[index];
            ITypeParameterSymbol implementationParameter = implementationParameters[index];

            if (implementationParameter.HasReferenceTypeConstraint && !HasReferenceTypeGuarantee(targetParameter))
                return true;
            if (implementationParameter.HasValueTypeConstraint && !HasValueTypeGuarantee(targetParameter))
                return true;
            if (implementationParameter.HasUnmanagedTypeConstraint && !targetParameter.HasUnmanagedTypeConstraint)
                return true;
            if (implementationParameter.HasConstructorConstraint && !HasConstructorGuarantee(targetParameter))
                return true;

            foreach (ITypeSymbol requiredConstraint in implementationParameter.ConstraintTypes)
            {
                if (!IsConstraintGuaranteed
                    (
                        targetParameter,
                        requiredConstraint,
                        targetParameters,
                        implementationParameters
                    ))
                    return true;
            }
        }

        return false;
    }

    static bool HasGenericConstraintMismatch
    (
        ImmutableArray<ITypeParameterSymbol> targetParameters,
        ImmutableArray<ITypeParameterSymbol> implementationParameters
    )
    {
        return HasUnsatisfiedGenericConstraints(targetParameters, implementationParameters) ||
            HasUnsatisfiedGenericConstraints(implementationParameters, targetParameters);
    }

    static bool HasReferenceTypeGuarantee(ITypeParameterSymbol parameter)
    {
        return parameter.HasReferenceTypeConstraint || parameter.ConstraintTypes.Any(x => x.TypeKind == TypeKind.Class);
    }

    static bool HasValueTypeGuarantee(ITypeParameterSymbol parameter) => parameter.HasValueTypeConstraint || parameter.HasUnmanagedTypeConstraint;

    static bool HasConstructorGuarantee(ITypeParameterSymbol parameter) => parameter.HasConstructorConstraint || HasValueTypeGuarantee(parameter);

    static bool IsConstraintGuaranteed
    (
        ITypeParameterSymbol targetParameter,
        ITypeSymbol requiredConstraint,
        ImmutableArray<ITypeParameterSymbol> targetParameters,
        ImmutableArray<ITypeParameterSymbol> implementationParameters
    )
    {
        if (requiredConstraint.SpecialType == SpecialType.System_Object)
            return true;
        if (requiredConstraint.SpecialType == SpecialType.System_ValueType && HasValueTypeGuarantee(targetParameter))
            return true;

        foreach (ITypeSymbol targetConstraint in targetParameter.ConstraintTypes)
        {
            if (AreEquivalentTypes(targetConstraint, requiredConstraint, targetParameters, implementationParameters) ||
                IsAssignableConstraint(targetConstraint, requiredConstraint, targetParameters, implementationParameters))
                return true;
        }

        return false;
    }

    static bool IsAssignableConstraint
    (
        ITypeSymbol candidate,
        ITypeSymbol required,
        ImmutableArray<ITypeParameterSymbol> targetParameters,
        ImmutableArray<ITypeParameterSymbol> implementationParameters
    )
    {
        if (AreEquivalentTypes(candidate, required, targetParameters, implementationParameters))
            return true;
        if (candidate is not INamedTypeSymbol namedCandidate)
            return false;

        foreach (INamedTypeSymbol interfaceType in namedCandidate.AllInterfaces)
        {
            if (AreEquivalentTypes(interfaceType, required, targetParameters, implementationParameters))
                return true;
        }

        return namedCandidate.BaseType != null && IsAssignableConstraint
        (
            namedCandidate.BaseType,
            required,
            targetParameters,
            implementationParameters
        );
    }

    static bool AreEquivalentTypes
    (
        ITypeSymbol first,
        ITypeSymbol second,
        ImmutableArray<ITypeParameterSymbol> targetParameters,
        ImmutableArray<ITypeParameterSymbol> implementationParameters
    )
    {
        if (first is ITypeParameterSymbol || second is ITypeParameterSymbol)
        {
            if (first is not ITypeParameterSymbol targetParameter || second is not ITypeParameterSymbol implementationParameter)
                return false;

            int targetIndex = GetParameterIndex(targetParameter, targetParameters);
            int implementationIndex = GetParameterIndex(implementationParameter, implementationParameters);
            return targetIndex >= 0 && targetIndex == implementationIndex;
        }

        if (SymbolEqualityComparer.Default.Equals(first, second))
            return true;

        if (first is INamedTypeSymbol firstNamed && second is INamedTypeSymbol secondNamed)
        {
            if (!SymbolEqualityComparer.Default.Equals(firstNamed.OriginalDefinition, secondNamed.OriginalDefinition) || firstNamed.TypeArguments.Length != secondNamed.TypeArguments.Length)
                return false;

            for (int index = 0; index < firstNamed.TypeArguments.Length; index++)
            {
                if (!AreEquivalentTypes(firstNamed.TypeArguments[index], secondNamed.TypeArguments[index], targetParameters, implementationParameters))
                    return false;
            }

            return true;
        }

        if (first is IArrayTypeSymbol firstArray && second is IArrayTypeSymbol secondArray)
            return firstArray.Rank == secondArray.Rank && AreEquivalentTypes(firstArray.ElementType, secondArray.ElementType, targetParameters, implementationParameters);

        if (first is IPointerTypeSymbol firstPointer && second is IPointerTypeSymbol secondPointer)
            return AreEquivalentTypes(firstPointer.PointedAtType, secondPointer.PointedAtType, targetParameters, implementationParameters);

        return false;
    }

    static int GetParameterIndex(ITypeParameterSymbol parameter, ImmutableArray<ITypeParameterSymbol> parameters)
    {
        for (int index = 0; index < parameters.Length; index++)
        {
            if (SymbolEqualityComparer.Default.Equals(parameter, parameters[index]))
                return index;
        }

        return -1;
    }

    static bool HasMatchingRegistry
    (
        INamedTypeSymbol implementationType,
        INamedTypeSymbol registrationAttribute,
        INamedTypeSymbol attributedRegistry,
        ImmutableArray<ReferencedRegistryProperty> referencedRegistryProperties
    )
    {
        for (INamedTypeSymbol? current = implementationType; current != null; current = current.BaseType)
        {
            foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!IsCurrentGeneratedRegistryProperty(property, attributedRegistry) || !MatchesRegistry(property, implementationType, registrationAttribute, GetCurrentRegistryBaseType(property)))
                    continue;

                return true;
            }
        }

        foreach (ReferencedRegistryProperty registry in referencedRegistryProperties)
        {
            if (!MatchesRegistry(registry.property, implementationType, registrationAttribute, registry.baseType))
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
            attribute.AttributeClass != null &&
            GeneratorUtils.GetMetadataName(attribute.AttributeClass) == generateTypeRegistryAttributeMetadataName);
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

    static bool MatchesRegistry(IPropertySymbol property, INamedTypeSymbol implementationType, INamedTypeSymbol registrationAttribute, ITypeSymbol? baseType)
    {
        if (baseType == null || property.Type is not INamedTypeSymbol registryType || registryType.TypeArguments.Length != 1)
            return false;

        return TypeRegistrySymbolHelpers.IsSameOrDerived(implementationType, baseType) &&
            TypeRegistrySymbolHelpers.IsSameOrDerived(registrationAttribute, registryType.TypeArguments[0]);
    }

    static ITypeSymbol? GetCurrentRegistryBaseType(IPropertySymbol property)
    {
        foreach (AttributeData attribute in property.GetAttributes())
        {
            if (attribute.AttributeClass == null || GeneratorUtils.GetMetadataName(attribute.AttributeClass) != generateTypeRegistryAttributeMetadataName)
                continue;

            if (attribute.ConstructorArguments.Length != 0 && attribute.ConstructorArguments[0].Value is ITypeSymbol baseType)
                return baseType;

            return property.ContainingType;
        }

        return null;
    }

    static ImmutableArray<ReferencedRegistryProperty> FindReferencedRegistryProperties(Compilation compilation)
    {
        ImmutableArray<ReferencedRegistryProperty>.Builder properties = ImmutableArray.CreateBuilder<ReferencedRegistryProperty>();
        HashSet<IPropertySymbol> seenProperties = new(SymbolEqualityComparer.Default);

        foreach (IAssemblySymbol assembly in EnumerateReferencedAssemblies(compilation))
        {
            foreach (AttributeData manifest in assembly.GetAttributes())
            {
                if (manifest.AttributeClass == null || GeneratorUtils.GetMetadataName(manifest.AttributeClass) != typeRegistryManifestAttributeMetadataName)
                    continue;
                if (manifest.ConstructorArguments.Length < 2 ||
                    manifest.ConstructorArguments[0].Value is not INamedTypeSymbol ownerType ||
                    manifest.ConstructorArguments[1].Value is not string propertyName)
                    continue;

                IPropertySymbol? property = ownerType.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault();
                ITypeSymbol? baseType = manifest.ConstructorArguments.Length > 2 ? manifest.ConstructorArguments[2].Value as ITypeSymbol : ownerType;
                if (property != null && baseType != null && seenProperties.Add(property))
                    properties.Add(new ReferencedRegistryProperty(property, baseType));
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
